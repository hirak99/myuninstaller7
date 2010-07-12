using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MyUninstaller7 {
    public class StateComparer {
        public List<string> onlyIn1, onlyIn2;
        public void Compare(string file1, string file2, Action<int,object> Report) {
            List<string>[] record = new List<string>[] { new List<string>(), new List<string>() };
            using (GZipReader gzr1 = new GZipReader(file1))
            using (GZipReader gzr2 = new GZipReader(file2)) {
                TextReader[] sr = new TextReader[2] { gzr1.Reader, gzr2.Reader };
                string[] lastPath = new string[2] {@"\\\\", @"\\\\"};
                string[] line = new string[2];
                line[0] = sr[0].ReadLine();
                line[1] = sr[1].ReadLine();
                // how many comparisons to make before updating the progress
                // if i could get number of lines, i could set it to nLines/100 to perfoem exactly 100 updates
                long reportResolution = gzr2.Length/1000;
                if (reportResolution < 1) reportResolution = 1;
                long comparisons = 0;
                while (true) {
                    if ((comparisons++)%reportResolution==0)
                        Report(0, new MyTuple<long, long>(gzr2.Position, gzr2.Length));
                    if (line[0] == null && line[1] == null) break;
                    if (line[0] == line[1]) {
                        line[0] = sr[0].ReadLine();
                        line[1] = sr[1].ReadLine();
                    }
                    else {
                        int toExport;
                        if (line[1] == null || line[0].CompareTo(line[1]) < 0) {
                            toExport = 0;
                        }
                        else {  // if (line1 == null || line2.CompareTo(line1) < 0)
                            toExport = 1;
                        }
                        if (!line[toExport].StartsWith(lastPath[toExport])) {
                            record[toExport].Add(line[toExport]);
                            if (line[toExport][line[toExport].Length - 1] == '\\')
                                lastPath[toExport] = line[toExport];
                        }
                        // load the next line
                        line[toExport] = sr[toExport].ReadLine();
                    }
                }
            }
            onlyIn1 = record[0];
            onlyIn2 = record[1];
        }
    }
}
