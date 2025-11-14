using System;
using System.Buffers.Text;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public interface IAzureSqlDatabase
    {
        Task EnsureSchemaAsync();
        Task UpsertCustomerAsync(CustomerDto c);
        Task CreateUserAsync(string email, string password);
        Task AssignRoleAsync(string email, string roleName);
        Task<AuthResult> AuthenticateAsync(string email, string password);
    }

    public sealed class AzureSqlDatabase : IAzureSqlDatabase
    {
        private readonly string _cs;
        public AzureSqlDatabase(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("AzureSql") ?? throw new InvalidOperationException("Missing ConnectionStrings:AzureSql");
        }

        public async Task EnsureSchemaAsync()
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            // WHY: Idempotent schema creation for marks-aligned features (customers, users, roles, orders)
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"
IF OBJECT_ID('dbo.Customers') IS NULL
BEGIN
  CREATE TABLE dbo.Customers(
    CustomerId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PartitionKey NVARCHAR(64) NOT NULL,
    RowKey       NVARCHAR(64) NOT NULL UNIQUE,
    FirstName    NVARCHAR(100) NOT NULL,
    LastName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(256) NOT NULL UNIQUE,
    Phone        NVARCHAR(50) NULL,
    CreatedUtc   DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
  );
END;

IF OBJECT_ID('dbo.Users') IS NULL
BEGIN
  CREATE TABLE dbo.Users(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash VARBINARY(256) NOT NULL,
    PasswordSalt VARBINARY(128) NOT NULL,
    CreatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
  );
END;

IF OBJECT_ID('dbo.Roles') IS NULL
BEGIN
  CREATE TABLE dbo.Roles(
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(128) NOT NULL UNIQUE
  );
  INSERT INTO dbo.Roles(Name) SELECT v FROM (VALUES (N'Admin'),(N'Customer')) s(v)
  WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles);
END;

IF OBJECT_ID('dbo.UserRoles') IS NULL
BEGIN
  CREATE TABLE dbo.UserRoles(
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY(UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId) ON DELETE CASCADE
  );
END;

IF OBJECT_ID('dbo.Orders') IS NULL
BEGIN
  CREATE TABLE dbo.Orders(
    OrderId BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(32) NOT NULL DEFAULT N'Pending',
    CreatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Customers(CustomerId)
  );
END;

IF OBJECT_ID('dbo.OrderItems') IS NULL
BEGIN
  CREATE TABLE dbo.OrderItems(
    OrderItemId BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderId BIGINT NOT NULL,
    Sku NVARCHAR(64) NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    Qty INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE
  );
END;
";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpsertCustomerAsync(CustomerDto c)
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            var sql = @"
MERGE dbo.Customers AS tgt
USING (SELECT @CustomerId AS CustomerId) AS src
ON tgt.CustomerId = src.CustomerId
WHEN MATCHED THEN UPDATE SET
  PartitionKey=@PartitionKey, RowKey=@RowKey, FirstName=@FirstName, LastName=@LastName, Email=@Email, Phone=@Phone
WHEN NOT MATCHED THEN INSERT(CustomerId, PartitionKey, RowKey, FirstName, LastName, Email, Phone, CreatedUtc)
VALUES(@CustomerId,@PartitionKey,@RowKey,@FirstName,@LastName,@Email,@Phone,SYSUTCDATETIME());";

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@CustomerId", c.CustomerId);
            cmd.Parameters.AddWithValue("@PartitionKey", c.PartitionKey);
            cmd.Parameters.AddWithValue("@RowKey", c.RowKey);
            cmd.Parameters.AddWithValue("@FirstName", c.FirstName);
            cmd.Parameters.AddWithValue("@LastName", c.LastName);
            cmd.Parameters.AddWithValue("@Email", c.Email);
            cmd.Parameters.AddWithValue("@Phone", (object?)c.Phone ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CreateUserAsync(string email, string password)
        {
            var (hash, salt) = HashPassword(password);
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            await using var cmd = cn.CreateCommand();
            cmd.CommandText = @"IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Email=@Email)
INSERT INTO dbo.Users(Email,PasswordHash,PasswordSalt) VALUES(@Email,@Hash,@Salt);";
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.Add("@Hash", SqlDbType.VarBinary, hash.Length).Value = hash;
            cmd.Parameters.Add("@Salt", SqlDbType.VarBinary, salt.Length).Value = salt;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AssignRoleAsync(string email, string roleName)
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            int userId, roleId;
            await using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT UserId FROM dbo.Users WHERE Email=@Email";
                cmd.Parameters.AddWithValue("@Email", email);
                userId = (int?)await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("User not found");
            }
            await using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT RoleId FROM dbo.Roles WHERE Name=@Name";
                cmd.Parameters.AddWithValue("@Name", roleName);
                roleId = (int?)await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Role not found");
            }
            await using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles WHERE UserId=@U AND RoleId=@R)
INSERT INTO dbo.UserRoles(UserId,RoleId) VALUES(@U,@R);";
                cmd.Parameters.AddWithValue("@U", userId);
                cmd.Parameters.AddWithValue("@R", roleId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            byte[]? hash = null, salt = null;
            int? userId = null;
            await using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT UserId, PasswordHash, PasswordSalt FROM dbo.Users WHERE Email=@Email";
                cmd.Parameters.AddWithValue("@Email", email);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    userId = r.GetInt32(0);
                    hash = (byte[])r["PasswordHash"];
                    salt = (byte[])r["PasswordSalt"];
                }
            }
            if (userId is null || hash is null || salt is null)
                return new AuthResult { Success = false, FailureReason = "Invalid email/password" };

            if (!VerifyPassword(password, hash, salt))
                return new AuthResult { Success = false, FailureReason = "Invalid email/password" };

            var roles = new List<string>();
            await using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"SELECT r.Name FROM dbo.UserRoles ur
JOIN dbo.Roles r ON r.RoleId = ur.RoleId
WHERE ur.UserId=@U;";
                cmd.Parameters.AddWithValue("@U", userId);
                await using var rr = await cmd.ExecuteReaderAsync();
                while (await rr.ReadAsync()) roles.Add(rr.GetString(0));
            }
            return new AuthResult { Success = true, Roles = roles.ToArray() };
        }

        // --- Password helpers (PBKDF2) ---
        private static (byte[] hash, byte[] salt) HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return (hash, salt);
        }

        private static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}
