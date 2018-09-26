// Author: Patrick Lee, https://www.linkedin.com/in/patrick-lee-4854994/, patrick.lee@inqa.com 
// Date: 26 Sep 2018
// Comments: written for Zupatech as example code
// File:		BookingWebApp, HomeController (MVC controller for user interface)
// Project:		For demonstration only
// Description:	Example web app allowing users to book seats for a monthly meeting assuming 10 by 10 seats
//				
// Author:		Patrick Lee (PJL), https://www.linkedin.com/in/patrick-lee-4854994/, patrick.lee@inqa.com 
//
// Copyright: please attribute to Patrick Lee for any reuse 
//********************************************************************************
// Date		   Who	Description
// 26 Sep 2018 PJL  Written for Zupatech as example code
// Obvious improvements that could be made: add validation that email addresses match regular expression for email addresses
// Also, at the moment, the React Class Component (App) doesn't update immediately after a change for some reason
// (probably due to my only having started React 6 days ago, and the course not covering updating state after an Ajax call)
// It only updates after the user types something into a text box, or clicks a checkbox
// Allow user to select from a list of available (future) meeting dates (currently hard coded to 30 Sep 2018)
// Available meeting dates could be stored in a separate Azure Table (querying to find a complete list of all PartitionKeys in
// an Azure table is problematic. Alternatively, could instead search existing Azure Table for all PartitionKeys for dates in the next year)
// allow user to view (read only) bookings for past meeting dates


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.WindowsAzure.Storage; // added for CloudStorageAccount
using System.Configuration; // 06 Jun 2015 added for ConfigurationManager
using Microsoft.WindowsAzure.Storage.Table; // needed for CloudTable
using BookingWebApp.Models; // added for MeetupBooking

namespace BookingWebApp.Controllers
{
    // for a production version, this would require SSL (added 26 Sep 2018 in FilterConfig.cs)
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Bookings()
        {
            // In a production version (where users have to be logged in to access this page),
            // if the user who is logged in is a member of the group with admin rights for bookings,
            // set this to true instead
            ViewBag.userHasAdminRights = true;// false;

            return View();
        }

        //public async Task<ActionResult> GetJsonProjectionDataForOutput(DateTime startDate, DateTime endDate, int familyID, int memberID)
        public ActionResult GetJsonBookingDataFromDatabase(String desiredDateAsISOString)
        {
            // In a production version users would have to be logged in to access this method

            var table = GetTheRequiredCloudTable();

            // Create the table query.
            TableQuery<MeetupBooking> query = new TableQuery<MeetupBooking>().Where(TableQuery.GenerateFilterCondition("PartitionKey", 
                QueryComparisons.Equal, desiredDateAsISOString));
            var results = table.ExecuteQuery(query);

            // Loop through the results, displaying information about the entity.
            foreach (MeetupBooking entity in results)
            {
                Console.WriteLine("{0}, {1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey,
                    entity.FullName, entity.SeatBooked);
            }

            // 16 Sep 2016 we need try and catch handlers on any Ajax method for which an exception might be thrown, otherwise the 
            // browser page may be left hanging
            try
            {
                QueryOutput queryOutput = new QueryOutput();
                queryOutput.MeetupBookings = results.ToList();
                return Json(queryOutput);
            } //  end try
            catch (Exception ex)
            {
                // 11 Feb 2014 amended to use this very useful function (otherwise too often the error message is meaningless)
                String errorMessage = GetErrorMessageIfNecessaryFromNestedInnerExceptions(ex);
                return Json(new { errors = new { message = errorMessage } });
            }
        }

        // In a production version users would have to be logged in to access this method
        public ActionResult AjaxPostBooking(MeetupBooking booking)
        {
            // 16 Sep 2016 we need try and catch handlers on any Ajax method for which an exception might be thrown, otherwise the 
            // browser page may be left hanging
            try
            {
                BookingsController bookingsController = new BookingsController();
                String requiredSecret = ConfigurationManager.AppSettings["ClientSecret"];
                RequestResponse response =
                    bookingsController.PostBooking(requiredSecret, booking);
                return Json(response);
            } //  end try
            catch (Exception ex)
            {
                // 11 Feb 2014 amended to use this very useful function (otherwise too often the error message is meaningless)
                String errorMessage = GetErrorMessageIfNecessaryFromNestedInnerExceptions(ex);
                return Json(new { errors = new { message = errorMessage } });
            }
        }

        // In a production version users would have to be logged in to access this method
        public ActionResult AjaxPostBookings(List<MeetupBooking> bookings)
        {
            // 16 Sep 2016 we need try and catch handlers on any Ajax method for which an exception might be thrown, otherwise the 
            // browser page may be left hanging
            try
            {
                BookingsController bookingsController = new BookingsController();
                String requiredSecret = ConfigurationManager.AppSettings["ClientSecret"];
                bool useBatch = true;
                RequestResponse response =
                    bookingsController.PostBooking(requiredSecret, useBatch, bookings);
                return Json(response);
            } //  end try
            catch (Exception ex)
            {
                // 11 Feb 2014 amended to use this very useful function (otherwise too often the error message is meaningless)
                String errorMessage = GetErrorMessageIfNecessaryFromNestedInnerExceptions(ex);
                return Json(new { errors = new { message = errorMessage } });
            }
        }


        // In a production version users would have to be logged in to access this method
        public ActionResult AjaxDeleteBooking(MeetupBooking booking)
        {
            // 16 Sep 2016 we need try and catch handlers on any Ajax method for which an exception might be thrown, otherwise the 
            // browser page may be left hanging
            try
            {
                BookingsController bookingsController = new BookingsController();
                String requiredSecret = ConfigurationManager.AppSettings["AdminClientSecret"];
                RequestResponse response =
                    bookingsController.Delete(requiredSecret, booking);
                return Json(response);
            } //  end try
            catch (Exception ex)
            {
                // 11 Feb 2014 amended to use this very useful function (otherwise too often the error message is meaningless)
                String errorMessage = GetErrorMessageIfNecessaryFromNestedInnerExceptions(ex);
                return Json(new { errors = new { message = errorMessage } });
            }
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Private procedures
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static CloudTable GetTheRequiredCloudTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Get a reference to the "MeetupBookings" table.
            CloudTable table = tableClient.GetTableReference("MeetupBookings");

            return table;
        }


        private string GetErrorMessageIfNecessaryFromNestedInnerExceptions(Exception ex)
        {
            String errorMessage = ex.Message;
            // 10 Feb 2014 added
            if (errorMessage.Contains("inner exception") && ex.InnerException != null)
            {
                Exception exToUse = ex;
                while (exToUse.Message.Contains("inner exception") && exToUse.InnerException != null)
                {
                    exToUse = exToUse.InnerException;
                    if (null == exToUse.InnerException)
                        errorMessage += ".  The innermost error message reported was: " + exToUse.Message;
                }
            }
            return errorMessage;
        }
    }
}