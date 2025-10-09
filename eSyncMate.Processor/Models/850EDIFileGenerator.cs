using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class _850EDIFileGenerator
    {


        private static int ControlNumber = 3;  // Example start number

        public class Segment
        {
            public string Name { get; set; }
            public List<string> Data { get; set; }
        }

        public class LineItem
        {
            public List<Segment> Data { get; set; }
        }

        public class EDIJson
        {
            public Segment ISA { get; set; }
            public Segment GS { get; set; }
            public Segment ST { get; set; }
            public List<Segment> StartNodes { get; set; }
            public List<LineItem> LINES { get; set; }
        }

        public string GenerateEDIFile(string json)
        {
            var ediJson = JsonConvert.DeserializeObject<EDIJson>(json);
            var ediString = new System.Text.StringBuilder();

            // Get the current date and time
            var currentDate = DateTime.Now.ToString("yyMMdd");
            var currentTime = DateTime.Now.ToString("HHmm");
            var controlNumberStr = ControlNumber.ToString("D9");

            // ISA Segment with dynamic date, time, and control number
            ediJson.ISA.Data[8] = currentDate;
            ediJson.ISA.Data[9] = currentTime;
            ediJson.ISA.Data[12] = controlNumberStr;

            // Formatting ISA with fixed-width fields and correct separators
            ediString.Append(ediJson.ISA.Name);
            ediString.Append($"*{ediJson.ISA.Data[0].PadRight(2)}");
            ediString.Append($"*{ediJson.ISA.Data[1].PadRight(10)}");
            ediString.Append($"*{ediJson.ISA.Data[2].PadRight(2)}");
            ediString.Append($"*{ediJson.ISA.Data[3].PadRight(10)}");
            ediString.Append($"*{ediJson.ISA.Data[4].PadRight(2)}");
            ediString.Append($"*{ediJson.ISA.Data[5].PadRight(15)}");
            ediString.Append($"*{ediJson.ISA.Data[6].PadRight(2)}");
            ediString.Append($"*{ediJson.ISA.Data[7].PadRight(15)}");
            ediString.Append($"*{ediJson.ISA.Data[8]}");
            ediString.Append($"*{ediJson.ISA.Data[9]}");
            ediString.Append($"*{ediJson.ISA.Data[10].PadRight(1)}");
            ediString.Append($"*{ediJson.ISA.Data[11].PadRight(5)}");
            ediString.Append($"*{ediJson.ISA.Data[12].PadRight(9)}");
            ediString.Append($"*{ediJson.ISA.Data[13].PadRight(1)}");
            ediString.Append($"*{ediJson.ISA.Data[14].PadRight(1)}");
            ediString.Append($"*{ediJson.ISA.Data[15].PadRight(1)}~\n");

            // GS Segment with dynamic date and time
            ediJson.GS.Data[3] = DateTime.Now.ToString("yyyyMMdd");
            ediJson.GS.Data[4] = currentTime;
            ediJson.GS.Data[5] = controlNumberStr;
            ediString.Append(ediJson.GS.Name);

            foreach (var dataElement in ediJson.GS.Data)
            {
                ediString.Append($"*{dataElement}");
            }
            ediString.Append("~\n");

            // ST Segment
            ediJson.ST.Data[1] = controlNumberStr;
            ediString.Append(ediJson.ST.Name);
            foreach (var dataElement in ediJson.ST.Data)
            {
                ediString.Append($"*{dataElement}");
            }
            ediString.Append("~\n");

            int segmentCount = 1;  // ST segment is already counted
            int po1Count = 0;      // Counter for PO1 segments

            // Other Segments
            foreach (var segment in ediJson.StartNodes)
            {
                ediString.Append(segment.Name);
                foreach (var dataElement in segment.Data)
                {
                    ediString.Append($"*{dataElement}");
                }
                ediString.Append("~\n");
                segmentCount++;
            }

            foreach (var line in ediJson.LINES)
            {
                foreach (var segment in line.Data)
                {
                    ediString.Append(segment.Name);
                    foreach (var dataElement in segment.Data)
                    {
                        ediString.Append($"*{dataElement}");
                    }
                    ediString.Append("~\n");
                    segmentCount++;

                    // Count PO1 segments
                    if (segment.Name == "PO1")
                    {
                        po1Count++;
                    }
                }
            }

            // CTT Segment (Total number of PO1 segments)
            ediString.Append($"CTT*{po1Count}~\n");
            segmentCount++;  // Count the CTT segment

            // SE Segment (number of segments including ST and SE, and control number)
            ediString.Append($"SE*{segmentCount + 1}*{controlNumberStr}~\n");

            // GE Segment (number of ST segments and control number)
            ediString.Append($"GE*1*{controlNumberStr}~\n");

            // IEA Segment (number of GS segments and control number)
            ediString.Append($"IEA*1*{controlNumberStr}~\n");

            // Increment the control number for the next file
            ControlNumber++;

            return ediString.ToString();
        }

    }

}