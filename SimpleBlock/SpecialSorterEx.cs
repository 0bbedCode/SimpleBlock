//using System.Linq;
//using System.Collections.Generic;
//using System.Reflection;

//namespace SimpleBlock {
//    internal class SpecialSorterEx {
//        public static string Types = "abcdefghijklmnopqrstuvwxyz0123456789";
//        public int Count = 0;
//        public int Dups = 0;

//        public Dictionary<char, List<Entry>> Storage = new Dictionary<char, List<Entry>>();
//        public SpecialSorterEx() {
//            for (var i = 0; i < Types.Length; i++)
//                Storage.Add(Types[i], new List<Entry>());
//        }

//        //how can we speed this up ....



//        //Check if we can make this all native ?
//        //Since we now go through the enteries out selfs its slower
//        public void Add(Entry entery) {
//            if (entery is null)
//                return;

//            unchecked {
//                var itm = entery.Host;
//                if (Storage.TryGetValue(itm[0], out var stor)) {
//                    var arr = (Entry[])typeof(List<Entry>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(stor);

//                    for(var i = 0; i < stor.Count; i++) {
//                        if (itm == arr[i].Host)
//                            return;
//                    }

//                    //stor.Contains()
//                    //for (var i = 0; i < stor.Count; i++) {
//                    //    if (itm == stor[i].Host)
//                    //        return;
//                    //}

//                    stor.Add(entery);
//                    Count++;
//                }
//                else {
//                    var strg = new List<Entry>();
//                    Storage.Add(itm[0], strg);
//                    strg.Add(entery);
//                    Count++;
//                }
//            }
//        }

//        public Entry[] ToArray() {
//            var arr = new Entry[Count];
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
