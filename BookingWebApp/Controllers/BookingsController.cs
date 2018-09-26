// Author: Patrick Lee, https://www.linkedin.com/in/patrick-lee-4854994/, patrick.lee@inqa.com 
// Date: 26 Sep 2018
// Comments: written for Zupatech as example code
// File:		BookingWebApp, BookingsController (WebApi controller to provide an API for anyone [subject to having correct clientSecret etc] to book/amend bookings)
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
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Microsoft.WindowsAzure.Storage; // added for CloudStorageAccount
using System.Configuration; // added for ConfigurationManager
using Microsoft.WindowsAzure.Storage.Table; // needed for CloudTable
using BookingWebApp.Models; // added for MeetupBooking

namespace BookingWebApp.Controllers
{
    // the [UseSSL] attribute is made available after adding UseSSLAttribute class in FilterConfig.cs in App_Start

    [UseSSL] // this is needed for security, otherwise the values of clientSecret or adminClientSecret could be intercepted
    public class BookingsController : ApiController
    {

        private List<String> _listOfAllSeats;

        /// <summary>
        /// Constructor, currently added only to set the list of all seats
        /// If this list needs to change, then the code here could be amended, or the code in this class 
        /// could be updated to read the list of seats from a table, depending on the chosen meeting date (or building etc)
        /// </summary>
        public BookingsController()
        {
            _listOfAllSeats = new List<string>();
            for (int row = 1; row <= 10; row++)
            {
                _listOfAllSeats.Add("A" + row);
                _listOfAllSeats.Add("B" + row);
                _listOfAllSeats.Add("C" + row);
                _listOfAllSeats.Add("D" + row);
                _listOfAllSeats.Add("E" + row);
                _listOfAllSeats.Add("F" + row);
                _listOfAllSeats.Add("G" + row);
                _listOfAllSeats.Add("H" + row);
                _listOfAllSeats.Add("I" + row);
                _listOfAllSeats.Add("J" + row);
            }
        }

        // example of use:
        // GET: api/Bookings?clientSecret=value
        /// <summary>
        /// Returns the top 100 records (in case the table is huge) in the Azure Table (irrespective of meeting date)
        /// (Probably not a useful method in practice, but included for completeness)
        /// </summary>
        /// <param name="clientSecret">Needed to restrict access to the API to authorised client apps only</param>
        /// <returns></returns>
        public IEnumerable<MeetupBooking> Get(String clientSecret)
        {
            String requiredValue = ConfigurationManager.AppSettings["ClientSecret"];
            if (clientSecret != requiredValue)
                return new List<MeetupBooking>();

                var table = GetTheRequiredCloudTable();
            int pageSize = 100; 
            TableQuery<MeetupBooking> query = new TableQuery<MeetupBooking>()
                .Take(pageSize)
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, ""));
            var results = table.ExecuteQuery(query);
            return results;
        }

        // example of use:
        // GET: api/Bookings/2018-09-30?clientSecret=value
        /// <summary>
        /// Returns all the current bookings for a current PartitionKey (meeting date)
        /// </summary>
        /// <param name="PartitionKey">Set this to the required meeting date (as an ISO string, e.g. 2018-09-30</param>
        /// <param name="clientSecret">Needed to restrict access to the API to authorised client apps only</param>
        /// <returns></returns>
        public IEnumerable<MeetupBooking> Get(String PartitionKey, String clientSecret)
        {
            String requiredValue = ConfigurationManager.AppSettings["ClientSecret"];
            if (clientSecret != requiredValue)
                return new List<MeetupBooking>();

            String dateInISOStringForm = PartitionKey;
            var table = GetTheRequiredCloudTable();
            TableQuery<MeetupBooking> query = 
                new TableQuery<MeetupBooking>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, dateInISOStringForm));
            var results = table.ExecuteQuery(query);
            return results;
        }

        // example of use:
        // GET: api/Bookings/2018-09-30?clientSecret=value&availableSeatsOnly=true
        /// <summary>
        /// third Get method to obtain list of seats (either all, or just the available ones) for a particular meeting date 
        /// </summary>
        /// <param name="PartitionKey">Set this to the required meeting date (as an ISO string, e.g. 2018-09-30</param>
        /// <param name="clientSecret">Needed to restrict access to the API to authorised client apps only</param>
        /// <param name="availableSeatsOnly">extra parameter to distinguish this method from the one above, also set to true if you just want a list of available seats</param>
        /// <returns>A list of string values containing the names of all seats, or just the available seats (if availableSeatsOnly = true)</returns>
        public IEnumerable<String> Get(String PartitionKey, String clientSecret, bool availableSeatsOnly)
        {
            String requiredValue = ConfigurationManager.AppSettings["ClientSecret"];
            if (clientSecret != requiredValue)
                return new List<String>();

            String dateInISOStringForm = PartitionKey;
            if (availableSeatsOnly)
                return GetAvailableSeats(dateInISOStringForm);

            return _listOfAllSeats;
        }

        // example of use:
        // POST: api/Bookings?clientSecret=value
        /// <summary>
        /// Adds a single booking record to the Table 
        /// </summary>
        /// <param name="clientSecret">Needed to restrict access to the API to authorised client apps only</param>
        /// <param name="booking">booking record to insert into the table</param>
        /// <returns>a RequestResponse object, with a Success boolean value and a Message (normally only used to provide meaningful information
        /// in the event of a failure, but could be used for success too if desired)
        /// </returns>
        public RequestResponse PostBooking(String clientSecret, MeetupBooking booking)
        {
            try
            {
                String requiredValue = ConfigurationManager.AppSettings["ClientSecret"];
                if (clientSecret != requiredValue)
                    throw new Exception("Permission denied");

                // check that business rules are satisfied
                RequestResponse response = RequestedNewBookingIsValid(booking);
                if (!response.Success)
                    return response;

                var table = GetTheRequiredCloudTable();
                TableOperation insertOperation = TableOperation.Insert(booking);
                table.Execute(insertOperation);

                // if have got here, then OK
                response.Message = "Record was added successfully";
                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        // example of use:
        // POST: api/Bookings?clientSecret=value&useBatch=true
        /// <summary>
        /// Adds a batch of recurds (currently maximum of 4, and all with the same date) to the table
        /// </summary>
        /// <param name="clientSecret">Needed to restrict access to the API to authorised client apps only</param>
        /// <param name="useBatch">a dummy parameter which is only added to differentiate this method from the PostBooking method where only one record is posted</param>
        /// <param name="bookings"> list of booking records (maximum 4) to insert into the table</param>
        /// <returns>a RequestResponse object, with a Success boolean value and a Message (normally only used to provide meaningful information
        /// in the event of a failure, but could be used for success too if desired)
        /// </returns>
        public RequestResponse PostBooking(String clientSecret, bool useBatch, List<MeetupBooking> bookings)
        {
            try
            {
                String requiredValue = ConfigurationManager.AppSettings["ClientSecret"];
                if (clientSecret != requiredValue)
                    throw new Exception("Permission denied");

                int MaxNumberOfSeatsPerBooking = 4;
                if (bookings.Count > MaxNumberOfSeatsPerBooking)
                    return new RequestResponse
                    {
                        Success = false,
                        Message = "A maximum of " + MaxNumberOfSeatsPerBooking + " seats may be booked per request"
                    };

                // Create the batch operation.
                TableBatchOperation batchOperation = new TableBatchOperation();

                // this checks that the entire batch is valid, if any one record is invalid, the batch operation is aborted
                Dictionary<Tuple<string, string>, string> dicEmailAddresses = new Dictionary<Tuple<string, string>, string>();
                Dictionary<Tuple<string, string>, string> dicFullNames = new Dictionary<Tuple<string, string>, string>();
                Dictionary<Tuple<string, string>, string> dicSeats = new Dictionary<Tuple<string, string>, string>();
                bool partitionKeySetYet = false;
                String partitionKey = "";
                foreach (var booking in bookings)
                {
                    if (!partitionKeySetYet)
                    {
                        partitionKey = booking.PartitionKey;
                        partitionKeySetYet = true;
                    }
                    else
                    {
                        if (booking.PartitionKey != partitionKey)
                            // this is a restriction which Microsoft impose for Azure Tables
                            throw new Exception("All records in the same batch must have the same partition key (i.e. meeting date)");
                    }

                    // check that business rules are satisfied
                    RequestResponse response = RequestedNewBookingIsValid(booking);
                    if (!response.Success)
                        return response;
                    // we also need to check that seats, email addresses and full names are unique (for a given date) within the batch
                    // NOTE: in actual fact, records in a batch for an Azure table all need to have the same partition key, so a tuple
                    // is not needed here, but this code would work for another storage method which didn't have this restriction (of same partition key)
                    Tuple<string, string> key = new Tuple<string, string>(booking.PartitionKey, booking.RowKey);
                    String field = "email address";
                    AddTupleKeyToDictionaryCheckingForUniqueness(key, field, dicEmailAddresses);
                    key = new Tuple<string, string>(booking.PartitionKey, booking.FullName);
                    field = "full name";
                    AddTupleKeyToDictionaryCheckingForUniqueness(key, field, dicFullNames);
                    key = new Tuple<string, string>(booking.PartitionKey, booking.SeatBooked);
                    field = "seat booked";
                    AddTupleKeyToDictionaryCheckingForUniqueness(key, field, dicSeats);

                    // if we have got here then this record looks OK
                    batchOperation.Insert(booking);
                }

                // Execute the batch operation.
                var table = GetTheRequiredCloudTable();
                table.ExecuteBatch(batchOperation);

                // if have got here, then OK
                return new RequestResponse
                {
                    Success = true,
                    Message = bookings.Count + " records were added successfully"
                };
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        // example of use:
        // PUT: api/Bookings?clientSecret=value&useBatch=true
        /// <summary>
        /// Updates a single existing record
        /// </summary>
        /// <param name="adminClientSecret">a different secret from clientSecret, on the grounds that only admin users should be able to edit or delete existing records</param>
        /// <param name="booking">updated values for desired booking record</param>
        /// <returns>a RequestResponse object, with a Success boolean value and a Message (normally only used to provide meaningful information
        /// in the event of a failure, but could be used for success too if desired)
        /// </returns>
        public RequestResponse Put(String adminClientSecret, MeetupBooking booking)
        {
            try
            {
                String requiredValue = ConfigurationManager.AppSettings["AdminClientSecret"];
                if (adminClientSecret != requiredValue)
                    return new RequestResponse
                    {
                        Success = false,
                        Message = "Permission denied"
                    };

                String dateInISOStringForm = booking.PartitionKey;
                String emailAddress = booking.RowKey;
                var table = GetTheRequiredCloudTable();
                // retrieve the requested record
                TableOperation retrieveOperation = TableOperation.Retrieve<MeetupBooking>(dateInISOStringForm, emailAddress);
                TableResult retrievedResult = table.Execute(retrieveOperation);
                MeetupBooking retrievedEntity = (MeetupBooking)retrievedResult.Result;

                if (retrievedEntity == null)
                    return new RequestResponse
                    {
                        Success = false,
                        Message = "The requested record does not exist"
                    };

                RequestResponse response = RequestedBookingChangeIsValid(retrievedEntity, booking);
                if (!response.Success)
                    return response;
                table = GetTheRequiredCloudTable();
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(booking);
                // Execute the operation. Because an entity already exists in the
                // table, its property values will be overwritten by those in the changed entity
                // If no entity with these keys existed, the entity would be
                // added to the table.
                table.Execute(insertOrReplaceOperation);
                return new RequestResponse
                {
                    Success = true,
                    Message = "Record was updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // example of use:
        // DELETE: api/Bookings?adminClientSecret=value
        /// <summary>
        /// Deletes a specified existing record from the table
        /// Note: an alternative would be to pass PartitionKey and RowKey (email address) as parameters (or in the route), but problems arise in URL encoding the email address
        /// so safer to pass this information in via the booking parameter
        /// </summary>
        /// <param name="adminClientSecret">a different secret from clientSecret, on the grounds that only admin users should be able to edit or delete existing records</param>
        /// <param name="booking">the booking record to delete (NB only the PartitionKey and RowKey are used to find the record in the table to delete)</param>
        /// <returns>a RequestResponse object, with a Success boolean value and a Message (normally only used to provide meaningful information
        /// in the event of a failure, but could be used for success too if desired)
        /// </returns>
        public RequestResponse Delete(String adminClientSecret, MeetupBooking booking)
        {
            try
            {
                String requiredValue = ConfigurationManager.AppSettings["AdminClientSecret"];
                if (adminClientSecret != requiredValue)
                    return new RequestResponse
                    {
                        Success = false,
                        Message = "Permission denied"
                    };

                String dateInISOStringForm = booking.PartitionKey;
                String emailAddress = booking.RowKey;
                var table = GetTheRequiredCloudTable();
                // retrieve the requested record
                TableOperation retrieveOperation = TableOperation.Retrieve<MeetupBooking>(dateInISOStringForm, emailAddress);
                TableResult retrievedResult = table.Execute(retrieveOperation);
                MeetupBooking retrievedEntity = (MeetupBooking)retrievedResult.Result;

                if (retrievedEntity == null)
                    return new RequestResponse
                    {
                        Success = false,
                        Message = "The requested record did not exist so could not be deleted"
                    };

                TableOperation deleteOperation = TableOperation.Delete(retrievedEntity);
                table.Execute(deleteOperation);

                // if have got here, then OK
                return new RequestResponse
                {
                    Success = true,
                    Message = "Record was deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Private procedures
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private IEnumerable<String> GetAvailableSeats(String dateInISOStringForm)
        {
            List<String> availableSeats = new List<string>();
            foreach (var seat in _listOfAllSeats)
                availableSeats.Add(seat);
            var table = GetTheRequiredCloudTable();
            TableQuery<MeetupBooking> query =
                new TableQuery<MeetupBooking>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, dateInISOStringForm));
            var results = table.ExecuteQuery(query);
            foreach (var item in results)
                availableSeats.Remove(item.SeatBooked);
            return availableSeats;
        }

        private RequestResponse RequestedNewBookingIsValid(MeetupBooking booking)
        {
            // initialise response as invalid for safety/defensive programming
            RequestResponse response = new RequestResponse { Success = false, Message = "" };

            String dateInISOStringForm = booking.PartitionKey;
            var availableSeats = GetAvailableSeats(dateInISOStringForm);

            String desiredSeat = booking.SeatBooked;
            if (null== desiredSeat || "" == desiredSeat)
                return new RequestResponse
                {
                    Success = false,
                    Message = "Seat must not be blank"
                };
            if (!_listOfAllSeats.Contains(desiredSeat))
                return new RequestResponse
                {
                    Success = false,
                    Message = "Seat " + desiredSeat + " is not a recognised seat for this meeting"
                };

            if (!availableSeats.Contains(desiredSeat))
                return new RequestResponse
                {
                    Success = false,
                    Message = "Seat " + desiredSeat + " is already booked"
                };

            response = userHasAlreadyBookedASeat(booking);
            if (!response.Success)
                return response;

            // add any future business rule checks here

            // if have got here then OK
            response.Success = true;
            return response;
        }

        private RequestResponse RequestedBookingChangeIsValid(MeetupBooking existing, MeetupBooking changed)
        {
            // initialise response as invalid for safety/defensive programming
            RequestResponse response = new RequestResponse { Success = false, Message = "" };

            // this should not happen but added for safety (belt and braces)
            if (existing.PartitionKey != changed.PartitionKey || existing.RowKey != changed.RowKey)
                return new RequestResponse
                {
                    Success = false,
                    Message = "Changed booking record's keys do not match existing record"
                };

            // we now need to check that the proposed new seat is not already taken
            String dateInISOStringForm = existing.PartitionKey;
            var availableSeats = GetAvailableSeats(dateInISOStringForm);
            String desiredSeat = changed.SeatBooked;
            if (!availableSeats.Contains(desiredSeat))
                return new RequestResponse
                {
                    Success = false,
                    Message = "Seat " + desiredSeat + " is already booked"
                };


            // add any future business rule checks here

            // if have got here then OK
            response.Success = true;
            return response;
        }

        private void AddTupleKeyToDictionaryCheckingForUniqueness(Tuple<String, String> key, 
            String field, Dictionary<Tuple<String, String>, String> dic)
        {
            if (dic.ContainsKey(key))
                throw new Exception("Duplicate in batch detected for combination date/" + field + " of " +
                    key.Item1 + "/" + key.Item2);
            dic.Add(key, ""); // the value is irrelevant here, we are only using the dictionary to check for uniqueness
        }

        private RequestResponse userHasAlreadyBookedASeat(MeetupBooking booking)
        {
            // initialise response as invalid for safety/defensive programming
            RequestResponse response = new RequestResponse { Success = false, Message = "" };

            String emailAddress = booking.RowKey;
            String fullName = booking.FullName;
            var table = GetTheRequiredCloudTable();
            String dateInISOStringForm = booking.PartitionKey;
            TableQuery<MeetupBooking> query = new TableQuery<MeetupBooking>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, dateInISOStringForm),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, emailAddress)));
            var results = table.ExecuteQuery(query);
            if (results.Count() > 0)
                return new RequestResponse
                {
                    Message = "User with email address = " +
                        emailAddress + " has already booked a seat for this meeting"
                };


            query = new TableQuery<MeetupBooking>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, dateInISOStringForm),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("FullName", QueryComparisons.Equal, fullName)));
            results = table.ExecuteQuery(query);
            if (results.Count() > 0)
                return new RequestResponse
                {
                    Message = "User with full name = " +
                        fullName + " has already booked a seat for this meeting"
                };

            // if have got here then OK
            response.Success = true;
            return response;
        }

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


    } // class
} // namepace
