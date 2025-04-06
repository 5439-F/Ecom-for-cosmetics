using Angular_Project_7.Server.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Angular_Project_7.Server.Services
{
    public class UserService
    {
        private readonly IMongoCollection<AppUser> _users;

        // Constructor that takes in MongoDBSettings and MongoClient
        public UserService(IOptions<MongoDBSettings> settings, IMongoClient client)
        {
            // Get the database from the MongoClient using the database name from settings
            var database = client.GetDatabase(settings.Value.DatabaseName);

            // Get the "Users" collection from the database
            _users = database.GetCollection<AppUser>("Users");
        }

        // Method to get a user by their email address
        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync(); // Ensure Email field is correct
            return user;
        }

        public bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            // Ensure you are comparing the hashed password correctly
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash); // Example using BCrypt hashing
        }


        // Method to create a new user in the database
        public async Task CreateUserAsync(AppUser user)
        {
            await _users.InsertOneAsync(user);
            Console.WriteLine("User inserted: " + user.Email); // Debugging output
        }
    }
}