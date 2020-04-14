// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.BotBuilderSamples
{
    // Extends the partial FlightBooking class with methods and properties that simplify accessing entities in the luis results
    public partial class IntentOperations
    {
        public (string From, string Airport) FromEntities
        {
            get
            {
                var fromValue = Entities?._instance?.From?.FirstOrDefault()?.Text;
                var fromAirportValue = Entities?.From?.FirstOrDefault()?.Airport?.FirstOrDefault()?.FirstOrDefault();
                return (fromValue, fromAirportValue);
            }
        }

        public (string SharepointSearch,string DocType) SPDocEntities
        {
            get
            {
               var DocName = Entities?._instance?.DocType?.FirstOrDefault()?.Text;

                var fromDocValue = Entities?._instance?.SharepointSearch?.FirstOrDefault()?.Text;
                return (fromDocValue, DocName);
            }
        }

        public (string personName, string email) ADEntities
        {
            get
            {
                var perName = Entities?._instance?.personName?.FirstOrDefault()?.Text;

                var emailId = Entities?._instance?.email?.FirstOrDefault()?.Text;
                return (perName, emailId);
            }
        }


        public (string To, string Airport) ToEntities
        {
            get
            {
                var toValue = Entities?._instance?.To?.FirstOrDefault()?.Text;
                var toAirportValue = Entities?.To?.FirstOrDefault()?.Airport?.FirstOrDefault()?.FirstOrDefault();
                return (toValue, toAirportValue);
            }
        }


        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
        public string TravelDate
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0];
    }
}
