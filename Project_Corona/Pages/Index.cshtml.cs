﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Project_Corona.Database;
using Project_Corona.Models;
using Npgsql;

namespace Project_Corona.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
        protected void LoginButton_Click(object sender, EventArgs e)
        {
            Response.Redirect("/admin");

        }
        public void LoginCheck(string email, string password)
        {
            try
			{
                var cs = "Host=localhost;Username=postgres;Password=admin;Database=Corona kantoor app";
				using var con = new NpgsqlConnection(cs);
                con.Open();
		
                using var cmd = new NpgsqlCommand();
                cmd.Connection = con;

                cmd.CommandText = @"SELECT * FROM employee WHERE email = @email AND password = @password";

                string emailInput = Request.Form["email"];
                string passwordInput = Request.Form["password"];



                

            }
            catch (Exception)
			{

				throw;
			}
			
        }
    }
}