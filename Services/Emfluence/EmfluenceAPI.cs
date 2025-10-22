using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStudioDataMgr.Services.Emfluence
{
    public class EmfluenceAPI
    {
        public class RootResponse
        {
            public DataWrapper Data { get; set; }
            public int Success { get; set; }
            public int Code { get; set; }
            public string RequestID { get; set; }
        }

        public class DataWrapper
        {
            public List<Record> Records { get; set; }
            public Paging Paging { get; set; }
        }

        public class Record
        {
            public int TemplateID { get; set; }
            public string ReplyToAddress { get; set; }
            public int UserID { get; set; }
            public DateTime DateModified { get; set; }
            public string FromAddress { get; set; }
            public string DeliveryType { get; set; }
            public string Subject { get; set; }
            public DateTime DateAdded { get; set; }
            public List<int> CampaignIDs { get; set; }
            public Metrics Metrics { get; set; }
            public string ParentEmailID { get; set; }
            public string Status { get; set; }
            public Schedule Schedule { get; set; }
            public string AbSplitID { get; set; }
            public string FromName { get; set; }
            public long EmailID { get; set; }
            public string ReplyToName { get; set; }
            public string Title { get; set; }
            public DateTime DateSent { get; set; }
        }

        public class Metrics
        {
            public int Clicks { get; set; }
            public int UniqueViews { get; set; }
            public int Bounces { get; set; }
            public int Forwards { get; set; }
            public int Recipients { get; set; }
            public int UniqueForwards { get; set; }
            public double ClickToViewRate { get; set; }
            public int SendingIssues { get; set; }
            public int WebViews { get; set; }
            public int Complaints { get; set; }
            public int Shares { get; set; }
            public int UniqueClicks { get; set; }
            public int Unsubscribes { get; set; }
            public int UniqueWebViews { get; set; }
            public int Views { get; set; }
            public int UniqueShares { get; set; }
        }

        public class Schedule
        {
            public string DateScheduled { get; set; }
            public int Confirmed { get; set; }
        }

        public class Paging
        {
            public int TotalPages { get; set; }
            public string Next { get; set; }
            public int Rpp { get; set; }
            public string Prev { get; set; }
            public int Page { get; set; }
            public int TotalRecords { get; set; }
        }
    }
}
