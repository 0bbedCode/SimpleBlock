//using System.Linq;
//using System.Collections.Generic;
//using System.Diagnostics;

//namespace SimpleBlock {
//    internal class FastStringStorage {
//        public static string Types = "abcdefghijklmnopqrstuvwxyz0123456789";
//        public int Count = 0;
//        public int Dups = 0;

//        public Dictionary<char, HashSet<string>> Storage = new Dictionary<char, HashSet<string>>();
//        public FastStringStorage() {
//            for (var i = 0; i < Types.Length; i++)
//                Storage.Add(Types[i], new HashSet<string>());
//        }

//        public void Add(string str) {
//            if (str is null)
//                return;

//            unchecked {
//                if (Storage.TryGetValue(str[0], out var stor)) {
//                    if (stor.Add(str)) {
//                        Count++;
//                    }
//                }
//                else {
//                    var strg = new HashSet<string>();
//                    Storage.Add(str[0], strg);
//                    strg.Add(str);
//                    Count++;
//                }
//            }
//        }

//        public void AddRange(List<string> strs) {
//            unchecked {
//                for (var i = 0; i < strs.Count; i++) 
//                    Add(strs[i]);
//            }
//        }

//        public string[] ToArray() {
//            var arr = new string[Count];
//            var ix = 0;

//            for (var i = 0; i < Storage.Count; i++) {
//                var el = Storage.ElementAt(i);
//                var cnt = el.Value.Count;
//                if (cnt > 0) {
//                    el.Value.CopyTo(arr, ix);
//                    ix += cnt;
//                }
//            }

//            return arr;
//        }
//    }
//}
