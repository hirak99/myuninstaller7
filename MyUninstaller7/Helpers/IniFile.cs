using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MyUninstaller7 {
    /// <summary>
    /// Create a New INI file to store or load data
    /// Credits: http://www.codeproject.com/KB/cs/cs_ini.aspx
    /// </summary>
    public class IniFile {
        public string path;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);
        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="PathName"></PARAM>
        public IniFile(string PathName) {
            path = PathName;
        }

        public void WriteString(string Section, string Key, string Value) {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public string ReadString(string Section, string Key) {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            return temp.ToString();
        }

        public void WriteBool(string Section, string Key, bool Value) {
            WriteString(Section, Key, Value ? "1" : "0");
        }
        public bool ReadBool(string Section, string Key) {
            string s = ReadString(Section, Key).Trim();
            if (s.Length == 0 || s == "0") return false;
            else return true;
        }

        public void WriteInt(string Section, string Key, int Value) {
            WriteString(Section, Key, Value.ToString());
        }
        public int ReadInt(string Section, string Key, int Default) {
            string s = ReadString(Section, Key).Trim();
            if (s.Length == 0) return Default;
            else {
                try {
                    return int.Parse(s);
                } catch (FormatException) {
                    return Default;
                }
            }
        }
    }
}