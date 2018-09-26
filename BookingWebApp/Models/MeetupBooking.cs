using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table; // needed for TableEntity

namespace BookingWebApp.Models
{
    public class MeetupBooking: TableEntity
    {
        public MeetupBooking(string dateAndTime, string emailAddress)
        {
            this.PartitionKey = dateAndTime;
            this.RowKey = emailAddress;
        }

        public MeetupBooking() { }

        // must be unique for a given PartitionKey and RowKey
        public string FullName { get; set; }

        public string SeatBooked { get; set; }

        //  not needed yet, but this and other fields can easily be added later if desired
        // because the Azure Table allows this: records can have different fields (on top of the PartitionKey and RowKey)
        //public double FeePaid { get; set; }

    } // class


    public class QueryOutput
    {
        public List<MeetupBooking> MeetupBookings { get; set; }

    } // QueryOutput

} // namespace