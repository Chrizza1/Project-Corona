using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using myWebApp.Database;
using myWebApp.Models;
using Npgsql;


namespace myWebApp.Pages
{
    public class AddEmployeeModel : PageModel
    {
        private readonly ILogger<AddEmployeeModel> _logger;

        public AddEmployeeModel(ILogger<AddEmployeeModel> logger)
        {
            _logger = logger;
        }

        public string Info { get; set; }
 
        public void OnGet()
        {
        }
 
        public void OnPostSubmit(EmployeeModel employee)
        {
            this.Info = string.Format("Successfully saved, {0}", employee.Name);
            string emPassword = sha256_hash(employee.Password);
            CreateEmployee(employee.Name, employee.Email, emPassword, employee.Function);
        }

        public static string sha256_hash(string valueToEncrypt)
        {
            StringBuilder stringbuilder = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(valueToEncrypt));

                foreach(byte b in result)
                {
                    stringbuilder.Append(b.ToString());
                }
            }
            return stringbuilder.ToString();
        }
    
        public void CreateEmployee(string Name, string Email, string Password, string Function)
        { 
            var cs = Database.Database.Connector();

            using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "INSERT INTO employees(name, email, password, function) VALUES(@Name, @Email, @Password, @Function)";
            using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("name", Name);
            cmd.Parameters.AddWithValue("email", Email);
            cmd.Parameters.AddWithValue("password", Password);
            cmd.Parameters.AddWithValue("function", Function);

            cmd.Prepare();

            cmd.ExecuteNonQuery();
            con.Close();   
        }    
    }
}