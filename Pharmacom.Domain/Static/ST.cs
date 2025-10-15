using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Static
{
    public static class ST
    {
        public static List<string> Statuses = new List<string>
        {
            "Pending",
            "Approved",
            "Rejected",
            "Completed",
            "Payment Received"
        };
        public static string Pending = "Pending";
        public static string Approved = "Approved";
        public static string Rejected = "Rejected";
        public static string Completed = "Completed";
        public static string PaymentReceived = "Payment Received";

    }
}