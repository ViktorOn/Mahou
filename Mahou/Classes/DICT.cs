// CODED by BladeMight 18.01.2020 0:16
using System;
using System.Collections.Generic;

namespace Mahou {
	public struct KV<T> {
		public T k;
		public T v;
	}
	public class DICT<T> {
		public List<T> Key;
		public List<T> Val;
		public int len;
		public DICT() {
			Key = new List<T>();
			Val = new List<T>();
			len = 0;
		}
		public DICT(List<T> ks, List<T>vs) {
			if (ks.Count != vs.Count) {
				throw new Exception("Keys and Values count: Keys: "+ks.Count+", Values: "+vs.Count+" mismatch.");
			}
			Key = ks;
			Val = vs;
			len = Key.Count;
		}
		public DICT(Dictionary<T,T> dd) {
			if (dd.Count > 0) {
				Key = new List<T>(dd.Keys);
				Val = new List<T>(dd.Values);
				len = Key.Count;
			}
		}
		public DICT(T[] ks, T[] vs) {
			if (ks.Length != vs.Length) {
				throw new Exception("Keys and Values lenght: Keys: "+ks.Length+", Values: "+vs.Length+" mismatch.");
			}
			Key = new List<T>(ks);
			Val = new List<T>(vs);
			len = Key.Count;
		}
		public void Add(T k, T v) {
			Key.Add(k);
			Val.Add(v);
			len++;
		}
		public KV<T> Get(int i) {
			if (i>len-1) throw new Exception("Index: "+i+"(count) is outside of the length: "+len);
			return new KV<T>(){k = Key[i], v = Val[i]};
		}
		public T Get(T s) {
			var io = Key.IndexOf(s);
			if (io != -1)
				return Val[io];
			throw new Exception("Can't find key: "+s);
		}
		public void Set(T kk, T vv) {
			var io = Key.IndexOf(kk);
			if (io>len-1) {
				throw new Exception("DICT has only " + len + " length, while you wanted to set the "+io+"'th element.");
			}
			if (io != -1) {
				Val[io] = vv;
			} else {
				Add(kk, vv);
			}
		}
		public void Set(KV<T> kv, int i) {
			if (i>len-1) {
				throw new Exception("DICT has only " + len + " length, while you wanted to set the "+i+"'th element.");
			}
			Key[i] = kv.k;
			Val[i] = kv.v;
		}
		public void Rem(int i = -1, int range_start = -1, int range_end = -1) {
			if (len == 0) return;
			if (range_start > -1 && range_end > -1) {
				if (range_start<len && range_end<len) {
					for(;range_start != range_end; range_start++)
						Rem(range_start);
				} else throw new Exception("Remove range outside length: "+len+", range:"+range_start+"-"+range_end);
			}
			if (i == -2) return;
			if (i == -1) {
				Key.RemoveAt(Key.Count-1);
				Val.RemoveAt(Val.Count-1);
			} else {
				if (i>len) throw new Exception("Remove elements outside of length: "+len+" of DICT: "+i);
				Key.RemoveAt(i);
				Val.RemoveAt(i);
			}
			len--;
		}
		public void Clear() {
			Key.Clear();
			Val.Clear();
			len = 0;
		}
		public KV<T> this [int i] {
			get { return Get(i); }
			set { Set(value, i); }
		}
		public T this [T v] {
			get { return Get(v); }
			set { Set(v, value); }
		}
//		public KV<T> operator +(KV kv) {
//			Add(kv.k, kv.v);
//		}
//		public static DICT<T> operator +(DICT<T> nothing){
//			Add(kv.k, kv.v);
//		}
//		public static DICT<T> operator --(DICT<T> nothing){
//			Rem();
//		}
	}
}
