using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using myWebApp.Database;
using myWebApp.Models;
using myWebApp.Pages;
using Npgsql;


namespace myWebApp.Pages
{
    public class EmployeeIndexModel : PageModel
    {
        private readonly ILogger<EmployeeIndexModel> _logger;
        
        public EmployeeIndexModel(ILogger<EmployeeIndexModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public List<WorkspaceModel> locations {get; set;}
        [BindProperty]
        public List<WorkspaceModel> rooms {get; set;}

        

        public string Info { get; set; }
        public string userEmail {get; set;}
        public int Count {get; set;}

        public string Priodays {get; set;}

        public string Monday {get; set;}
        public string Tuesday {get; set;}
        public string Wednesday {get; set;}
        public string Thursday {get; set;}
        public string Friday {get; set;}
        public string Saturday {get; set;}
        public string Sunday {get; set;}

        public void OnGet()
        {
            userEmail = HttpContext.Session.GetString("useremail");
            locations = PopulateReservations();
            rooms = ShowRoom();
            Count = ShowNotification();
            bool Check = OpeningHoursModel.CheckIfExist();
            if(Check)
            {
                Tuple<string, string, string, string, string, string, string> hours = OpeningHoursModel.GetOpeningHours();
                Monday = hours.Item1;
                Tuesday = hours.Item2;
                Wednesday = hours.Item3;
                Thursday = hours.Item4;
                Friday = hours.Item5;
                Saturday = hours.Item6;
                Sunday = hours.Item7;
            }
            else
            {
                Monday = "Closed";
                Tuesday = "Closed";
                Wednesday = "Closed";
                Thursday = "Closed";
                Friday = "Closed";
                Saturday = "Closed";
                Sunday = "Closed";
            }
            Priodays = GetPriodays(userEmail);
        } 
        public void  OnPostSubmit(ReservationModel reservation)
        {
            userEmail = HttpContext.Session.GetString("useremail");
            locations = PopulateReservations();
            rooms = ShowRoom();
            Count = ShowNotification();
            Priodays = GetPriodays(userEmail);
            bool Check = OpeningHoursModel.CheckIfExist();
            if(Check)
            {
                Tuple<string, string, string, string, string, string, string> hours = OpeningHoursModel.GetOpeningHours();
                Monday = hours.Item1;
                Tuesday = hours.Item2;
                Wednesday = hours.Item3;
                Thursday = hours.Item4;
                Friday = hours.Item5;
                Saturday = hours.Item6;
                Sunday = hours.Item7;
            }
            
            else
            {
                Monday = "Closed";
                Tuesday = "Closed";
                Wednesday = "Closed";
                Thursday = "Closed";
                Friday = "Closed";
                Saturday = "Closed";
                Sunday = "Closed";
            }
            DateTime convdayid = Convert.ToDateTime(reservation.Date);
            
            bool check = prioCheck(reservation);
            bool check1 = CheckReservation(convdayid, userEmail);
            bool check2 = CheckRoomAvailability(reservation);
            bool check3 = CheckRoomOpen(convdayid);
            bool check4 = OpeningHoursModel.CheckIfExist();
            if(check && check1 && check2 && check3 && check4){
              CreateReservation(convdayid, reservation.Room, userEmail, reservation.Location);
              this.Info = string.Format("Reservation successfully saved");
            }
            else{
              if(!check){ 
                this.Info = string.Format("You do not have the right priority, please try a later date");
              }
              if (!check1) {
              this.Info = string.Format("You already have a reservation for this day, or you tried to reserve in the past, try a different date.");
              }
              if(!check2){
                this.Info = string.Format("The room you tried to reserve is full!");
              }
              if(!check3 || !check4) {
                this.Info = string.Format("This location is closed on this day, try another day");
              }
            }
        }

        public IActionResult OnPostShowRoom(string loc)
        {
            List<WorkspaceModel> l= new List<WorkspaceModel>();
            var cs = Database.Database.Connector();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select room FROM workspaces WHERE location = '"+ loc +"'";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            l.Add(new WorkspaceModel { RoomName = dr["room"].ToString() });
                        }
                    }
                    con.Close();
                }
            }
            return new JsonResult(l);
        }

        public static string GetPriodays(string userEmail)
        {
            string UserPrio = GetUserPrio(userEmail);
            bool CheckForPriorities = PrioritiesModel.CheckIfExist();
            if(CheckForPriorities == false)
            {
                if(UserPrio == "High")
                {
                    return "You can always reserve";
                }
                if(UserPrio == "Medium")
                {
                    return "You can reserve 7 days in advance";
                }
                if(UserPrio == "Low")
                {
                    return "You can reserve 2 days in advance";
                }
            }
            if(CheckForPriorities == true)
            {
                Tuple<int, int, int> GetPrio = PrioritiesModel.GetPriorities();
                string high = GetPrio.Item1.ToString();
                string medium = GetPrio.Item2.ToString();
                string low = GetPrio.Item3.ToString();
                if(UserPrio == "High")
                {
                    return "You can reserve " + high +" days in advance";
                }
                if(UserPrio == "Medium")
                {
                    return "You can reserve " + medium + " days in advance";
                }
                if(UserPrio == "Low")
                {
                    return "You can reserve " + low + " days in advance";
                }
            }
            return "";
        }

        public static string GetUserPrio(string userEmail)
        {
            var cs = Database.Database.Connector();

            using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "SELECT * FROM employees";
            using var cmd = new NpgsqlCommand(sql, con);

            NpgsqlDataReader dRead = cmd.ExecuteReader();
            
            while (dRead.Read())
            {
                for(int i = 0; i < dRead.FieldCount; i++)
                {
                    if(dRead[1].ToString() == userEmail)
                    {
                        return dRead[4].ToString();
                    }
                }
            }
            return null;
        }
        

        public bool prioCheck(ReservationModel reservation)
          {
            int med = 7;
            int low = 2;
            int high = 0;
            bool check = PrioritiesModel.CheckIfExist();
            if(check)
            {
                Tuple<int, int, int> getprio = PrioritiesModel.GetPriorities();
                med = getprio.Item2;
                low = getprio.Item3;
                high = getprio.Item1;
            }

          DateTime convdayid = Convert.ToDateTime(reservation.Date);
          userEmail = HttpContext.Session.GetString("useremail");
          
          var cs = Database.Database.Connector(); // start connectie met database

          List<string> pr = new List<string>(); //list creeren

          using var con = new NpgsqlConnection(cs);
          {
            string query = "Select priority FROM employees WHERE email = '"+ userEmail+"'";
            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            {
              cmd.Connection = con;
              con.Open();
              using (NpgsqlDataReader dr = cmd.ExecuteReader())
              {
                while (dr.Read())
                {
                  pr.Add(((string) dr["priority"]));
                }
              }
              con.Close(); //sluit de connection, maar de list pr bestaat nog
            }
          }

          foreach(string priority in pr)
          {
            if(priority == "Low") // 2 dagen van te voren
            {
              DateTime newdt = convdayid.AddDays(-(low));
              if(newdt <= DateTime.Now){return true;}
              else{return false;}
            }
            else if(priority == "Medium") // 7 dagen van te voren
            {
              DateTime newdt = convdayid.AddDays(-(med));
              if(newdt <= DateTime.Now){return true;}
              else{return false;}
            }
            else // high priority kan altijd reserveren
            {
              if(high != 0)
              {
                DateTime newdt = convdayid.AddDays(-(high));
                if(newdt <= DateTime.Now){return true;}
                else{return false;}
              }
              return true;
            }
          }
          return false;
        }

        public void OnPostRemove(ReservationModel reservation)
        {
            locations = PopulateReservations();
            rooms = ShowRoom();
            userEmail = HttpContext.Session.GetString("useremail");
            Count = ShowNotification();

            Priodays = GetPriodays(userEmail);
            
            DateTime convdayid = Convert.ToDateTime(reservation.Date);
            DeleteReservation(convdayid, reservation.Location);
            bool Check = OpeningHoursModel.CheckIfExist();
            if(Check)
            {
                Tuple<string, string, string, string, string, string, string> hours = OpeningHoursModel.GetOpeningHours();
                Monday = hours.Item1;
                Tuesday = hours.Item2;
                Wednesday = hours.Item3;
                Thursday = hours.Item4;
                Friday = hours.Item5;
                Saturday = hours.Item6;
                Sunday = hours.Item7;
            }
            
            else
            {
                Monday = "Closed";
                Tuesday = "Closed";
                Wednesday = "Closed";
                Thursday = "Closed";
                Friday = "Closed";
                Saturday = "Closed";
                Sunday = "Closed";
            }
        }
        public int ShowNotification()
        {
            
            
            var cs = Database.Database.Connector();
            List<string> not = new List<string>();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select bericht FROM counter";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        
                        while (dr.Read())
                        {
                            not.Add((string) dr["bericht"]);
                        }
                    }
                    
                    con.Close();
                }
            }
            
            foreach(string x in not) {
                
                Count++;
                
            }
            return Count;
        }
        public IActionResult OnGetRemoveCount() {
            var cs = Database.Database.Connector();
            using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "TRUNCATE counter ";
            using var cmd = new NpgsqlCommand(sql, con);
            cmd.Prepare();
            cmd.ExecuteNonQuery();
            con.Close();
            return new RedirectToPageResult("Notifications");

        }


        public void DeleteReservation(DateTime convdayid, string Location)
        {
            
            var cs = Database.Database.Connector();
            using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "DELETE FROM reservations WHERE res_location = @Location AND date = @Date";
            using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("Location", Location);
            cmd.Parameters.AddWithValue("Date", convdayid);
            cmd.Prepare();
            cmd.ExecuteNonQuery();
            con.Close();

        }

        public bool CheckReservation(DateTime convdayid, string Email) 
        {   
            int AmountDate = 0;
            DateTime now = DateTime.Now;
           
            if(now > convdayid){ return false;}
            
            var cs = Database.Database.Connector();
            List<DateTime> res = new List<DateTime>();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select date FROM reservations WHERE res_email = '"+ Email+"'";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            res.Add(((DateTime) dr["date"]));
                        }
                        
                    }
                    
                    
                }
            }
            foreach(DateTime p in res)
            {
                if (p == convdayid)
                {
                    AmountDate++;
                    
                }
                
            }
            if (AmountDate >= 1)
            {
                return false;
            }
            else{
                return true;
            }
            


        }
    public bool CheckRoomAvailability(ReservationModel reservation){
      var cs = Database.Database.Connector();
      using var con = new NpgsqlConnection(cs);
      con.Open();

      var sql = "SELECT COUNT(*) FROM reservations WHERE res_location = '"+reservation.Location+"' AND res_room = '"+reservation.Room+"' AND date = '"+reservation.Date+"'";
      using var cmd = new NpgsqlCommand(sql, con);

      int roomReservations = Convert.ToInt32(cmd.ExecuteScalar());
      sql = "Select availableworkspaces FROM workspaces WHERE location = '"+reservation.Location+"' AND room = '"+reservation.Room+"'";
      using var cmd2 = new NpgsqlCommand(sql, con);
      int roomAvailableSpaces = Convert.ToInt32(cmd2.ExecuteScalar());

      if(roomReservations < roomAvailableSpaces){ return true;}
      else{ return false;}
    }
    public bool CheckRoomOpen(DateTime date){
      int Day = (int)date.DayOfWeek;
      var cs = Database.Database.Connector();
      using var con = new NpgsqlConnection(cs);
      con.Open();

      var sql = "Select * FROM openinghours";
      using var cmd = new NpgsqlCommand(sql, con);
      NpgsqlDataReader dr = cmd.ExecuteReader();
      string open = "";
      while(dr.Read()){
        if(Day == 0){ open = dr[7].ToString();}
        else{ open = dr[Day].ToString();}
      }
      if(open.ToLower() == "closed"){ return false;}
      else{ return true;}
    }

        public void CreateReservation(DateTime convdayid, string Roomid, string Email, string Location) 
        {

            
            var cs = Database.Database.Connector();

            using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "INSERT INTO reservations(date, res_room, res_email, res_location) VALUES(@Date, @Room, @Email, @Location)";
            using var cmd = new NpgsqlCommand(sql, con);
            
            cmd.Parameters.AddWithValue("Date", convdayid);
            cmd.Parameters.AddWithValue("Room", Roomid);
            cmd.Parameters.AddWithValue("Email", Email);
            cmd.Parameters.AddWithValue("Location", Location);

            cmd.Prepare();

            cmd.ExecuteNonQuery();
            con.Close();  
        }

       

        

        public List<WorkspaceModel> PopulateReservations()
        {
            var cs = Database.Database.Connector();
            List<WorkspaceModel> res = new List<WorkspaceModel>();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select DISTINCT location FROM workspaces";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            res.Add(new WorkspaceModel { LocationName = dr["location"].ToString() });
                        }
                    }
                    
                    con.Close();
                }
            }


            return res;
        }

        public List<WorkspaceModel> ShowRoom()
        {
            var cs = Database.Database.Connector();
            List<WorkspaceModel> res = new List<WorkspaceModel>();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select room FROM workspaces";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            res.Add(new WorkspaceModel { RoomName = dr["room"].ToString() });
                        }
                    }
                    
                    con.Close();
                }
            }


            return res;
        }
        



        public List<ReservationModel> ShowReservation()
        {
            var cs = Database.Database.Connector();
            List<ReservationModel> res = new List<ReservationModel>();
            using var con = new NpgsqlConnection(cs);
            {
                string query = "Select date, res_location FROM reservations WHERE res_email = '"+ userEmail+"'";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                {
                    cmd.Connection = con;
                    con.Open();
                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        
                        while (dr.Read())
                        {
                            res.Add(new ReservationModel { Date = ((DateTime) dr["date"]).ToString("yyyy/MM/dd"), Location = dr["res_location"].ToString() });
                        }
                    }
                    
                    con.Close();
                }
            }


            return res;
        }



    }
}
        // Create
        
