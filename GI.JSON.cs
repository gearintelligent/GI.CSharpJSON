using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlTypes;

/*
 * Programming by GearIntellix
 */
namespace GI
{
    public enum JSONType
    {
        Null,
        String,
        Bool,
        Number,
        Object,
        Array,
        Unknown
    }

    public class JSON
    {
        JSONType _ctype;
        Object _cval;
        List<int> _cind;
        List<string> _ckey;
        List<JSON> _cchild;
        Dictionary<string, object> _opt;

        #region "Constructor"
        public JSON()
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Null;
        }
        public JSON(object val)
        {
            _opt = new Dictionary<string, object>();

            if (val is DateTime)
            {
                _ctype = JSONType.String;
                _cval = ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (val is SqlDateTime)
            {
                if (!((SqlDateTime)val).IsNull)
                {
                    _ctype = JSONType.String;
                    _cval = ((SqlDateTime)val).ToSqlString();
                }
                else _ctype = JSONType.Null;
            }
            else if (val is bool)
            {
                _ctype = JSONType.Bool;
                _cval = (bool)val;
            }
            else if (val is int || val is double || val is float || val is decimal)
            {
                _ctype = JSONType.Number;

                double outv = 0;
                if (!Double.TryParse(val.ToString(), out outv)) outv = 0;
                _cval = outv;
            }
            else if (val == null || val == DBNull.Value)
            {
                _ctype = JSONType.Null;
                _cval = null;
            }
            else if (val is List<object>)
            {
                List<object> co = (List<object>)val;

                _ctype = JSONType.Array;
                foreach (int c in co) this.ArrAdd(new JSON(co[c]));
            }
            else if (val is Dictionary<string, object>)
            {
                Dictionary<string, object> co = (Dictionary<string, object>)val;

                _ctype = JSONType.Object;
                foreach (var c in co) this.ObjAdd(c.Key, new JSON(c.Value));
            }
            else if (val is JSON)
            {
                JSON xj = (JSON)val;
                _arrMax = xj._arrMax;
                _cchild = xj._cchild;
                _ctype = xj._ctype;
                _cval = xj._cval;
                _ckey = xj._ckey;
            }
            else
            {
                _ctype = JSONType.String;
                _cval = StrFix(val.ToString());
            }
        }
        public JSON(object[] val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Array;
            foreach (int i in val) this.ArrAdd(new JSON(val[i]));
        }
        public JSON(string val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.String;
            _cval = StrFix(val);
        }
        public JSON(int val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Number;
            _cval = Convert.ToDouble(val);
        }
        public JSON(double val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Number;
            _cval = Convert.ToDouble(val);
        }
        public JSON(float val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Number;
            _cval = Convert.ToDouble(val);
        }
        public JSON(decimal val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Number;
            _cval = Convert.ToDouble(val);
        }
        public JSON(Boolean val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = JSONType.Bool;
            _cval = val;
        }
        public JSON(JSONType typ)
        {
            _opt = new Dictionary<string, object>();

            _ctype = typ;
            switch (typ)
            {
                case JSONType.Null:
                    _cval = null;
                    break;
                case JSONType.Bool:
                    _cval = false;
                    break;
                case JSONType.Number:
                    _cval = (double)0;
                    break;
                case JSONType.String:
                    _cval = "";
                    break;
                case JSONType.Object:
                    _ckey = new List<string>();
                    _cchild = new List<JSON>();
                    break;
                case JSONType.Array:
                    _cind = new List<int>();
                    _cchild = new List<JSON>();
                    break;
            }
        }
        public JSON(JSONType typ, object val)
        {
            _opt = new Dictionary<string, object>();

            _ctype = typ;
            switch (typ)
            {
                case JSONType.Null:
                    _cval = null;
                    break;
                case JSONType.Bool:
                    bool vb = false;
                    if (!bool.TryParse(val.ToString(), out vb)) vb = false;
                    _cval = vb;
                    break;
                case JSONType.Number:
                    double vn = 0;
                    if (!double.TryParse(val.ToString(), out vn)) vn = 0;
                    _cval = vn;
                    break;
                case JSONType.String:
                    _cval = StrFix(val.ToString());
                    break;
                case JSONType.Unknown:
                    _cval = val;
                    break;
                case JSONType.Object:
                    _ckey = new List<string>();
                    _cchild = new List<JSON>();
                    break;
                case JSONType.Array:
                    _cind = new List<int>();
                    _cchild = new List<JSON>();
                    break;
            }
        }

        static public JSON Parse(string scr)
        {
            int mx = 0;
            return ParseV2(scr, 0, out mx);
        }

        /*
         * Parse V2 with indextor one-time processing
         * Peformance up to 300% than before (V1)
         */
        static private JSON ParseV2(string scr, int frm, out int pos)
        {
            List<char> whitespace = new List<char>(new char[] { ' ', '\n', '\t', '\r' });
            JSON dcur = new JSON(JSONType.Null);
            int len = scr.Length;
            string ikey = null;
            char state = '\0';
            string cur = "";

            // [0] = false, [1] = true, [2] = done
            int quote = 0;

            int i = frm;
            while (len > i)
            {
                char c = scr[i];
                char n = (scr.Length > (i + 1) ? scr[i + 1] : '\0');

                if (quote == 1)
                {
                    int ii = scr.IndexOf("\"", i);
                    if (ii < 0) throw new Exception("Quote no ending [position: " + i + "]");
                    else
                    {
                        cur += scr.Substring(i, ii - i);
                        i = ii;

                        if (scr[ii - 1] == '\\')
                        {
                            i += 1;
                            cur += '"';
                            continue;
                        }
                        cur = StrEscape(cur, 3);
                        n = (scr.Length > (i + 1) ? scr[i + 1] : '\0');
                        c = scr[i];
                    }
                }
                else
                {
                    if (whitespace.IndexOf(c) > 0)
                    {
                        i += 1;
                        continue;
                    }
                }

                JSON cv = new JSON();
                bool ok = true;
                switch (c)
                {
                    case '{':
                    case '[':
                        switch (state)
                        {
                            case '\0':
                                switch (c)
                                {
                                    case '[':
                                        dcur = new JSON(JSONType.Array);
                                        break;
                                    case '{':
                                        dcur = new JSON(JSONType.Object);
                                        break;
                                    default:
                                        throw new Exception("Character '" + c + "' not valid [position: " + i + "]");
                                }
                                state = c;
                                break;
                            case '[':
                                dcur.ArrAdd(JSON.ParseV2(scr, i, out i));
                                break;
                            case '{':
                                if (ikey == null) throw new Exception("Key not setted [position: " + i + "]");
                                dcur[ikey] = JSON.ParseV2(scr, i, out i);
                                break;
                            default:
                                throw new Exception("Unknown stat [position: " + i + "]");
                        }
                        break;

                    case '}':
                    case ']':
                        if (cur != "")
                        {
                            switch (quote)
                            {
                                case 0:
                                    if (cur.Trim() != "")
                                    {
                                        switch (cur.Trim().ToLower())
                                        {
                                            case "null":
                                                // do nothing
                                                break;
                                            case "true":
                                                cv = new JSON(true);
                                                break;
                                            case "false":
                                                cv = new JSON(false);
                                                break;
                                            default:
                                                double cv2 = 0;
                                                if (!double.TryParse(cur, out cv2)) throw new Exception("Unknown constant " + cur + " [position: " + i + "]");
                                                cv = new JSON(cv2);
                                                break;
                                        }
                                    }
                                    else ok = false;
                                    break;
                                case 1:
                                    throw new Exception("Quote no ending [position: " + i + "]");
                                case 2:
                                    cv = new JSON(cur);
                                    break;
                            }
                        }
                        else ok = false;
                        quote = 0;
                        cur = "";

                        pos = i;
                        if (ok)
                        {
                            switch (state)
                            {
                                case '\0':
                                    throw new Exception("Character '" + c + "' not valid [position: " + i + "]");
                                case '[':
                                    if (c != ']') throw new Exception("Ending array not valid [position: " + i + "]");
                                    dcur.ArrAdd(cv);
                                    break;
                                case '{':
                                    if (c != '}') throw new Exception("Ending object not valid [position: " + i + "]");
                                    else if (ikey == null) throw new Exception("Key cannot be null [position: " + i + "]");
                                    dcur[ikey] = cv;
                                    break;
                                default:
                                    throw new Exception("Unknown stat [position: " + i + "]");
                            }
                        }
                        return dcur;

                    case '"':
                        if (quote == 0)
                        {
                            if (cur != "") throw new Exception("Value '" + cur + "' not valid [position: " + i + "]");
                            else quote = 1;
                        }
                        else if (quote == 1) quote = 2;
                        else throw new Exception("Quote not valid [position: " + i + "]");
                        break;

                    case ':':
                        if (state == '\0') throw new Exception("Character '" + c + "' not valid [position: " + i + "]");
                        else if (state == '[') throw new Exception("Character '" + c + "' just for object [position: " + i + "]");
                        else if (quote != 2) throw new Exception("Key must be string [position: " + i + "]");
                        ikey = cur;

                        quote = 0;
                        cur = "";
                        break;

                    case ',':
                        switch (quote)
                        {
                            case 0:
                                if (cur.Trim() != "")
                                {
                                    switch (cur.Trim().ToLower())
                                    {
                                        case "null":
                                            // do nothing
                                            break;
                                        case "true":
                                            cv = new JSON(true);
                                            break;
                                        case "false":
                                            cv = new JSON(false);
                                            break;
                                        default:
                                            double cv2 = 0;
                                            if (!double.TryParse(cur, out cv2)) throw new Exception("Unknown constant " + cur + " [position: " + i + "]");
                                            cv = new JSON(cv2);
                                            break;
                                    }
                                }
                                else ok = false;
                                break;
                            case 1:
                                throw new Exception("Quote not valid [position: " + i + "]");
                            case 2:
                                cv = new JSON(cur);
                                break;
                        }

                        if (ok)
                        {
                            switch (state)
                            {
                                case '\0':
                                    throw new Exception("Character '" + c + "' not valid [position: " + i + "]");
                                case '{':
                                    if (ikey == null) throw new Exception("Key cannot be null [position: " + i + "]");
                                    dcur[ikey] = cv;
                                    break;
                                case '[':
                                    dcur.ArrAdd(cv);
                                    break;
                                default:
                                    throw new Exception("Unknown stat [position: " + i + "]");
                            }
                        }
                        ikey = null;
                        quote = 0;
                        cur = "";
                        break;

                    default:
                        if (whitespace.IndexOf(c) < 0) cur += c;
                        break;
                }
                i += 1;
            }
            pos = i;
            return dcur;
        }
        
        static public JSON ParseFromURLData(string scr)
        {
            JSON dat = new JSON(JSONType.Object);
            string[] ls = scr.Split('&');
            foreach (string c in ls)
            {
                string[] ls2 = c.Split('=');
                if (ls2.Length >= 2 && ls2.First().Length > 0)
                {
                    string tmp = "";
                    for (int i = 1; i < ls2.Length; i++)
                    {
                        if (tmp != "") tmp += "=";
                        tmp += ls2[i];
                    }
                    dat.ObjAdd(Uri.UnescapeDataString(ls2.First()), new JSON(Uri.UnescapeDataString(tmp)));
                }
                else if (ls2.Length == 1 && ls2.First().Length > 0) dat.ObjAdd(Uri.UnescapeDataString(ls2.First()), new JSON(JSONType.Null));
            }
            return dat;
        }

        static public JSON ParseFromDataSet(DataSet dset, bool asArray)
        {
            return ParseFromDataSet(dset, asArray);
        }
        static public JSON ParseFromDataSet(DataSet dset, bool asArray, JSON opt)
        {
            if (opt == null) opt = new JSON(JSONType.Object);
            else if (opt.Type != JSONType.Object) opt = new JSON(JSONType.Object);

            JSON jdat = new JSON((asArray ? JSONType.Array : JSONType.Object));
            foreach (DataTable dtbl in dset.Tables)
            {
                if (asArray) jdat.ArrAdd(JSON.ParseFromDataTable(dtbl, opt));
                else jdat[dtbl.TableName] = JSON.ParseFromDataTable(dtbl, opt);
            }
            foreach (string k in opt.ObjKeys) jdat._opt[k] = opt[k].Value;
            return jdat;
        }

        static public JSON ParseFromDataTable(DataTable dtbl)
        {
            return ParseFromDataTable(dtbl, new JSON());
        }
        static public JSON ParseFromDataTable(DataTable dtbl, JSON opt)
        {
            if (opt == null) opt = new JSON(JSONType.Object);
            else if (opt.Type != JSONType.Object) opt = new JSON(JSONType.Object);

            JSON jdat = new JSON(JSONType.Array);
            foreach (DataRow dr in dtbl.Rows)
            {
                JSON sdat = new JSON(JSONType.Object);
                foreach (DataColumn dc in dtbl.Columns)
                {
                    object dat = dr[dc.ColumnName];
                    if (dc.DataType == typeof(string) && opt.ObjExists("trim") && opt["trim"].BoolValue) dat = dat.ToString().Trim();

                    sdat[dc.ColumnName] = new JSON(dat);
                }
                jdat.ArrAdd(sdat);
            }
            foreach (string k in opt.ObjKeys) jdat._opt[k] = opt[k].Value;
            return jdat;
        }

        static public JSON ParseFromDataRow(DataRow drow)
        {
            return ParseFromDataRow(drow, new JSON());
        }
        static public JSON ParseFromDataRow(DataRow drow, JSON opt)
        {
            if (opt == null) opt = new JSON(JSONType.Object);
            else if (opt.Type != JSONType.Object) opt = new JSON(JSONType.Object);

            JSON jdat = new JSON(JSONType.Object);
            if (drow.Table != null)
            {
                foreach (DataColumn dc in drow.Table.Columns)
                {
                    object dat = drow[dc.ColumnName];
                    if (dc.DataType == typeof(string) && opt.ObjExists("trim") && opt["trim"].BoolValue) dat = dat.ToString().Trim();

                    jdat[dc.ColumnName] = new JSON(dat);
                }
            }
            foreach (string k in opt.ObjKeys) jdat._opt[k] = opt[k].Value;
            return jdat;
        }

        static public class Tools
        {
            /*
             * Converting big data to JSON string instantly
             */
            static public string ConvertDataTableToJSONString(DataTable dtbl)
            {
                return ConvertDataTableToJSONString(dtbl, null);
            }
            static public string ConvertDataTableToJSONString(DataTable dtbl, JSON opt)
            {
                if (opt == null) opt = new JSON(JSONType.Object);
                else if (opt.Type != JSONType.Object) opt = new JSON(JSONType.Object);

                if (dtbl != null && dtbl.Rows.Count > 0)
                {
                    StringBuilder strx = new StringBuilder();
                    strx.Append("[");
                    for (int i = 0; i < dtbl.Rows.Count; i++)
                    {
                        DataRow dr = dtbl.Rows[i];
                        JSON jrow = new JSON(JSONType.Object);

                        // Options
                        if (opt.ObjExists("trim") && opt["trim"].BoolValue) jrow.StrTrim = true;

                        foreach (DataColumn dc in dtbl.Columns) jrow[dc.ColumnName] = new JSON(dr[dc.ColumnName]);
                        if (i > 0) strx.Append(",");
                        strx.Append(jrow.ToString());
                    }
                    strx.Append("]");
                    return strx.ToString();
                }
                else return "[]";
            }
        }
        #endregion;

        #region "Properties"
        public bool StrTrim
        {
            get
            {
                object v = this.GetProperty("trim");
                return (v == null ? false : (v is bool ? (bool)v : false));
            }
            set
            {
                this.SetProperty("trim", value);
            }
        }

        public object GetProperty(string name)
        {
            if (_opt == null) _opt = new Dictionary<string, object>();
            return (_opt.ContainsKey(name) ? _opt[name] : null);
        }

        public void SetProperty(string name, object value)
        {
            if (_opt == null) _opt = new Dictionary<string, object>();
            _opt[name] = value;
            if (_cchild != null) foreach (var c in _cchild) this.ApplyProperties(this, c, true);
        }
        #endregion

        #region "Getter / Setter"
        public JSON this[int index]
        {
            get
            {
                if (_ctype == JSONType.Array) return ArrGet(index);
                else if (_ctype == JSONType.Object) return ObjGet(index.ToString());
                else return null;
            }
            set
            {
                if (_ctype == JSONType.Array) ArrSet(index, value);
                else if (_ctype == JSONType.Object) ObjSet(index.ToString(), value);
            }
        }
        public JSON this[int index, JSON def]
        {
            get
            {
                if (_ctype == JSONType.Array) return ArrGet(index, def);
                else if (_ctype == JSONType.Object) return ObjGet(index.ToString(), def);
                else return def;
            }
        }
        public JSON this[string key]
        {
            get
            {
                if (_ctype == JSONType.Array)
                {
                    int i = -1;
                    if (int.TryParse(key, out i)) return ArrGet(i);
                    else return null;
                }
                else if (_ctype == JSONType.Object) return ObjGet(key);
                else return null;
            }
            set
            {
                if (_ctype == JSONType.Array)
                {
                    int i = 0;
                    if (int.TryParse(key, out i)) ArrSet(i, value);
                }
                else if (_ctype == JSONType.Object) ObjSet(key, value);
            }
        }
        public JSON this[string key, JSON def]
        {
            get
            {
                if (_ctype == JSONType.Array)
                {
                    int i = -1;
                    if (int.TryParse(key, out i)) return ArrGet(i, def);
                    else return def;
                }
                else if (_ctype == JSONType.Object) return ObjGet(key, def);
                else return def;
            }
        }

        public JSON ArrGet(int index)
        {
            if (_ctype != JSONType.Array) return null;
            int i = _cind.IndexOf(index);
            if (index >= 0 && index < GetArrMax(false))
            {
                if (i >= 0) return _cchild[i];
                else return new JSON(JSONType.Null);
            }
            return null;
        }
        public JSON ArrGet(int index, JSON def)
        {
            if (_ctype != JSONType.Array) return def;
            int i = _cind.IndexOf(index);
            if (index >= 0 && index < GetArrMax(false))
            {
                if (i >= 0) return _cchild[i];
                else return new JSON(JSONType.Null);
            }
            return def;
        }

        public JSON ObjGet(string key)
        {
            if (_ctype != JSONType.Object) return null;
            var i = _ckey.IndexOf(key);
            if (i >= 0) return _cchild[i];
            else return null;
        }
        public JSON ObjGet(string key, JSON def)
        {
            if (_ctype != JSONType.Object) return def;
            else if (!this.ObjExists(key)) return def;
            else return this[key];
        }
        #endregion;

        #region "Manipulate"
        public bool ObjExists(string key)
        {
            if (_ctype != JSONType.Object) return false;
            return _ckey.IndexOf(key) >= 0;
        }
        public int ObjExists(string[] keys)
        {
            int i = 0;
            foreach (string c in keys) i += (this.ObjExists(c) ? 1 : 0);
            return i;
        }
        public bool ObjAdd(string key, JSON val)
        {
            if (_ctype != JSONType.Object) return false;
            if (_ckey.IndexOf(key) >= 0) return false;
            this.ApplyProperties(this, val, false);

            _ckey.Add(key);
            _cchild.Add(val);
            return true;
        }
        public bool ObjSet(string key, JSON val)
        {
            if (_ctype != JSONType.Object) return false;
            this.ApplyProperties(this, val, false);
            int i = _ckey.IndexOf(key);
            if (i >= 0)
            {
                _cchild[i] = val;
                return true;
            }
            else return ObjAdd(key, val);
        }
        public bool ObjDel(string key)
        {
            if (_ctype != JSONType.Object) return false;

            var i = _ckey.IndexOf(key);
            if (i >= 0)
            {
                _ckey.RemoveAt(i);
                _cchild.RemoveAt(i);
                return true;
            }
            else return false;
        }

        public bool ArrAdd(JSON val)
        {
            if (_ctype != JSONType.Array) return false;
            this.ApplyProperties(this, val, false);
            _cind.Add(GetArrMax(true));
            _cchild.Add(val);
            _arrMax = -1;
            return true;
        }
        public bool ArrSet(int index, JSON val)
        {
            if (_ctype != JSONType.Array) return false;
            this.ApplyProperties(this, val, false);
            int i = _cind.IndexOf(index);
            if (i < 0)
            {
                _cind.Add(index);
                _cchild.Add(val);
            }
            else
            {
                _cchild[i] = val;
            }
            return true;
        }
        public bool ArrDel(int index)
        {
            if (_ctype != JSONType.Array) return false;
            int i = _cind.IndexOf(index);
            if (index >= 0 && index < GetArrMax(true))
            {
                if (i >= 0)
                {
                    _cind.RemoveAt(i);
                    _cchild.RemoveAt(i);
                    _arrMax = -1;
                }
                return true;
            }
            else return false;
        }
        public int[] ArrFind(string field, object value)
        {
            if (_ctype == JSONType.Array)
            {
                List<int> i = new List<int>();
                for (int i2 = 0; i2 < this.Count; i2++)
                {
                    JSON cur = this[i2];
                    if (cur.Type == JSONType.Object && cur.ObjExists(field))
                    {
                        if (value == null && cur[field].Value == null) i.Add(i2);
                        else if (value.Equals(cur[field].Value)) i.Add(i2);
                    }
                }
                return i.ToArray();
            }
            else return new int[] {};
        }
        public int[] ArrIFind(string field, object value)
        {
            if (_ctype == JSONType.Array)
            {
                List<int> i = new List<int>();
                for (int i2 = 0; i2 < this.Count; i2++)
                {
                    JSON cur = this[i2];
                    if (cur.Type == JSONType.Object && cur.ObjExists(field))
                    {
                        if (value == null && cur[field].Value == null) i.Add(i2);
                        else
                        {
                            if (value is string && value.ToString().ToLower() == cur[field].StringValue.ToLower()) i.Add(i2); 
                            else if (value.Equals(cur[field].Value)) i.Add(i2);
                        }
                    }
                }
                return i.ToArray();
            }
            else return new int[] { };
        }
        public bool Clear()
        {
            switch (_ctype)
            {
                case JSONType.Object:
                    _ckey.Clear();
                    _cchild.Clear();
                    return true;

                case JSONType.Array:
                    _cind.Clear();
                    _cchild.Clear();
                    return true;

                default: return false;
            }
        }

        public bool SetValue(JSONType typ, Object val)
        {
            _ctype = typ;
            double c0;
            JSON c1;

            switch (typ)
            {
                case JSONType.Null:
                    _cval = null;
                    break;
                case JSONType.Bool:
                    _cval = (bool)val ? true : false;
                    break;
                case JSONType.Number:
                    c0 = 0;
                    if (!double.TryParse(val.ToString(), out c0)) c0 = 0;
                    _cval = c0;
                    break;
                case JSONType.Object:
                case JSONType.Array:
                    if (val.GetType() != typeof(JSON)) return false;
                    c1 = (JSON)val;
                    if (c1.Type != typ) return false;
                    _cval = c1;
                    break;
                case JSONType.String:
                    _cval = val.ToString();
                    break;
                case JSONType.Unknown:
                    _cval = val;
                    break;
            }
            return true;
        }
        public bool SetValue(Object val)
        {
            return SetValue(_ctype, val);
        }

        public void SetType(JSONType to)
        {
            if (_ctype == to) return;

            _arrMax = -1;
            _ctype = to;

            switch (to)
            {
                case JSONType.Array:
                    switch (_ctype)
                    {
                        case JSONType.Object:
                            _ckey.Clear();
                            _cind = new List<int>();
                            for (int i = 0; i < _cchild.Count; i++)
                            {
                                _cind.Add(i);
                            }
                            break;
                        case JSONType.String:
                        case JSONType.Number:
                        case JSONType.Bool:
                            _cind = new List<int>();
                            _cchild = new List<JSON>();
                            this.ArrAdd(new JSON(_cval));
                            break;
                        case JSONType.Null:
                            _cind = new List<int>();
                            _cchild = new List<JSON>();
                            break;
                        case JSONType.Unknown:
                            _cind = new List<int>();
                            _cchild = new List<JSON>();
                            this.ArrAdd(new JSON(JSONType.Unknown, _cval));
                            break;
                    }
                    break;

                case JSONType.Object:
                    switch (_ctype)
                    {
                        case JSONType.Array:
                            _ckey = new List<string>();
                            foreach (int c in _cind) _ckey.Add(c.ToString());
                            break;
                        case JSONType.Unknown:
                        case JSONType.String:
                        case JSONType.Number:
                        case JSONType.Bool:
                        case JSONType.Null:
                            _ckey = new List<string>();
                            _cchild = new List<JSON>();
                            break;
                    }
                    break;

                case JSONType.String:
                    switch (_ctype)
                    {
                        case JSONType.Null:
                            _cval = "";
                            break;
                        default:
                            _cval = this.StringValue;
                            break;
                    }
                    break;

                case JSONType.Number:
                    switch (_ctype)
                    {
                        case JSONType.Object:
                        case JSONType.Array:
                            _cval = 0;
                            break;
                        case JSONType.Unknown:
                        case JSONType.String:
                            double cd = 0;
                            if (!double.TryParse(this.StringValue, out cd)) cd = 0;
                            _cval = 0;
                            break;
                        case JSONType.Bool:
                            _cval = (bool)_cval ? 1 : 0;
                            break;
                        case JSONType.Null:
                            _cval = 0;
                            break;
                    }
                    break;

                case JSONType.Bool:
                    switch (_ctype)
                    {
                        case JSONType.Object:
                        case JSONType.Array:
                            _cval = this.Count > 0;
                            break;
                        case JSONType.String:
                            _cval = _cval.ToString() != "";
                            break;
                        case JSONType.Number:
                            _cval = (double)_cval != 0;
                            break;
                        case JSONType.Null:
                            _cval = false;
                            break;
                        case JSONType.Unknown:
                            _cval = (_cval != null);
                            break;
                    }
                    break;

                case JSONType.Null:
                    _cval = null;
                    break;
            }
        }
        #endregion;

        #region "Properties"
        public bool IsNull
        {
            get
            {
                if (this._ctype != JSONType.Unknown) return (this.Type == JSONType.Null);
                else return true;
            }
        }

        public JSONType Type
        {
            get { return _ctype; }
        }

        public int Count
        {
            get
            {
                switch (_ctype)
                {
                    case JSONType.Object:
                        return _cchild.Count;

                    case JSONType.Array:
                        return this.GetArrMax(false);

                    default: return 0;
                }
            }
        }

        public string[] ObjKeys
        {
            get
            {
                if (_ctype != JSONType.Object) return new string[] { };
                return _ckey.ToArray();
            }
        }
        public int[] ArrIndexs
        {
            get
            {
                if (_ctype != JSONType.Array) return new int[] { };
                return _cind.ToArray();
            }
        }
        #endregion;

        #region "Functions"
        public object Value
        {
            get
            {
                if (this.StrTrim && _cval is string) return _cval.ToString().Trim();
                else return _cval;
            }
        }
        public string StringValue
        {
            get
            {
                string v = (_cval == null ? "" : _cval.ToString());
                return (this.StrTrim ? v.Trim() : v);
            }
        }
        public bool BoolValue
        {
            get
            {
                bool r = false;
                if (_cval == null || _cval == DBNull.Value) return false;

                var v = this.StringValue;
                if (bool.TryParse(v, out r)) return r;
                else return false;
            }
        }
        public double NumberValue
        {
            get
            {
                double r = 0;
                if (_cval == null || _cval == DBNull.Value) return 0;

                var v = this.StringValue;
                if (double.TryParse(v, out r)) return r;
                else return 0;
            }
        }

        public object DateTimeValue()
        {
            return DateTimeValue(DateTime.MinValue);
        }
        public object DateTimeValue(object defl)
        {
            try
            {
                return DateTime.Parse(this.StringValue);
            }
            catch (Exception) { return defl; }
        }
        public object GuidValue()
        {
            return GuidValue(Guid.Empty);
        }
        public object GuidValue(object defl)
        {
            try
            {
                return new Guid(this.StringValue);
            }
            catch (Exception) { return defl; }
        }

        public override string ToString()
        {
            string r = "";
            switch (_ctype)
            {
                case JSONType.Unknown:
                case JSONType.Null:
                    r = "null";
                    break;
                case JSONType.Number:
                case JSONType.String:
                    if (_cval == null) r = (_ctype == JSONType.String ? "" : "0");
                    else r = StrEscape(this.StringValue, 4);
                    if (_ctype == JSONType.String) r = "\"" + r + "\"";
                    break;
                case JSONType.Bool:
                    if ((bool)_cval == true) r = "true"; else r = "false";
                    break;
                case JSONType.Array:
                    r = "[";
                    foreach (JSON cur in _cchild)
                    {
                        if (cur.Type == JSONType.Unknown) continue;
                        if (r != "[") r += ",";
                        switch (cur.Type)
                        {
                            default:
                                r += cur.ToString();
                                break;
                        }
                    }
                    r += "]";
                    break;
                case JSONType.Object:
                    r = "{";
                    for (var i = 0; i < _ckey.Count; i++)
                    {
                        JSON cur = _cchild[i];
                        if (cur.Type == JSONType.Unknown) continue;

                        if (r != "{") r += ",";
                        r += "\"" + _ckey[i] + "\":";
                        switch (cur.Type)
                        {
                            default:
                                r += cur.ToString();
                                break;
                        }
                    }
                    r += "}";
                    break;
            }
            return r;
        }

        public string ToURLData()
        {
            return ToURLData("", true);
        }
        public string ToURLData(string name)
        {
            return ToURLData(name, true);
        }
        public string ToURLData(string name, bool escp)
        {
            if (escp && name != "") name = Uri.EscapeDataString(name);
            string r = (name != "" ? name + "=" : "");
            switch (_ctype)
            {
                case JSONType.Number:
                case JSONType.String:
                    r += Uri.EscapeDataString(this.StringValue);
                    break;
                case JSONType.Bool:
                    r += ((bool)_cval == true ? "1" : "0");
                    break;
                case JSONType.Array:
                    r = "";
                    for (var i = 0; i < _cind.Count; i++)
                    {
                        if (r != "") r += "&";
                        JSON cur = _cchild[i];
                        if (cur.Type == JSONType.Array || cur.Type == JSONType.Object)
                        {
                            r += cur.ToURLData(name + "[" + _cind[i] + "]", false);
                        }
                        else r += name + "[" + _cind[i] + "]=" + cur.ToURLData();
                    }
                    break;
                case JSONType.Object:
                    r = "";
                    for (var i = 0; i < _ckey.Count; i++)
                    {
                        if (r != "") r += "&";
                        JSON cur = _cchild[i];
                        if (cur.Type == JSONType.Array || cur.Type == JSONType.Object)
                        {
                            r += cur.ToURLData((name == "" ? "" : name + "[") + _ckey[i] + (name == "" ? "" : "]"), false);
                        }
                        else r += (name == "" ? "" : name + "[") + _ckey[i] + (name == "" ? "" : "]") + "=" + cur.ToURLData();
                    }
                    break;
            }
            return r;
        }

        public string ToSQLValue()
        {
            return this.ToSQLValue(false);
        }
        public string ToSQLValue(bool force)
        {
            switch (_ctype)
            {
                case JSONType.Array:
                case JSONType.Object:
                    if (force) return "'" + StrEscape(this.ToString(), 1) + "'";
                    else
                    {
                        string res = "";
                        foreach (JSON c in _cchild)
                        {
                            if (res != "") res += ", ";
                            res += c.ToSQLValue(true);
                        }
                        return res;
                    }

                case JSONType.String:
                    if (this.StringValue.Length > 0 && this.StringValue[0] == '!')
                    {
                        if (this.StringValue.Length > 1 && this.StringValue[1] == '!') return "'" + StrEscape(this.StringValue.Substring(1), 1) + "'";
                        else return this.StringValue.Substring(1);
                    } else return "'" + StrEscape(this.StringValue, 1) + "'";

                case JSONType.Number:
                    return StrEscape(this.StringValue.Replace(",", "."), 1);

                case JSONType.Bool:
                    return this.BoolValue ? "true" : "false";

                case JSONType.Null:
                default:
                    return "null";
            }
        }

        public string ToSQLSet()
        {
            string res = "";
            switch (_ctype)
            {
                case JSONType.Array:
                    foreach (int k in this.ArrIndexs)
                    {
                        if (res != "") res += ", ";
                        res += k + " = " + this[k].ToSQLValue(true);
                    }
                    break;

                case JSONType.Object:
                    foreach (string k in this.ObjKeys)
                    {
                        if (res != "") res += ", ";
                        res += k + " = " + this[k].ToSQLValue(true);
                    }
                    break;
            }
            return res;
        }

        public string ToSQLWhere()
        {
            string res = "";
            switch (_ctype)
            {
                case JSONType.Array:
                    foreach (int k in this.ArrIndexs)
                    {
                        if (res != "") res += ", ";
                        if (this[k].Type == JSONType.Null) res += k + " IS NULL";
                        else res += k + " = " + this[k].ToSQLValue(true);
                    }
                    break;

                case JSONType.Object:
                    foreach (string k in this.ObjKeys)
                    {
                        if (res != "") res += ", ";
                        if (this[k].Type == JSONType.Null) res += k + " IS NULL";
                        else res += k + " = " + this[k].ToSQLValue(true);
                    }
                    break;
            }
            return res;
        }

        static public string StrEscape(string val, int mode)
        {
            switch (mode)
            {
                // escape from slash
                case 0:
                    val = val.Replace("\\", "\\\\");
                    break;

                // escape for postgresql query
                case 1:
                    val = val.Replace("'", "''");
                    break;

                // escape from slash revert
                case 2:
                    bool nskip = false;
                    string nval = "";
                    int ipos = 0;
                    while (ipos < val.Length)
                    {
                        if (nskip)
                        {
                            nval += val[ipos];
                            nskip = false;
                        }
                        else if (val[ipos] == '\\') nskip = true;
                        else nval += val[ipos];
                        ipos += 1;
                    }
                    val = nval;
                    break;

                // descape slash
                case 3:
                    string res = "";
                    for (int i = 0; i < val.Length; i++)
                    {
                        if (val[i] == '\\') i += 1;
                        if (i < val.Length) res += val[i];
                    }
                    val = res;
                    break;

                // json escape
                case 4:
                    val = val.Replace("\\", "\\\\");
                    val = val.Replace("\"", "\\\"");
                    break;
            }
            return val;
        }
        #endregion;

        #region "Private Functions"
        private int _arrMax = -1;
        private int GetArrMax(bool force)
        {
            if (_arrMax < 0 || force)
            {
                _arrMax = 0;
                foreach (int cur in _cind)
                {
                    if (_arrMax < (cur + 1)) _arrMax = (cur + 1);
                }
            }
            return _arrMax;
        }
        private string StrFix(string val)
        {
            if (val == null) return null;
            val = val.Replace("\0", "");
            return val;
        }
        private void ApplyProperties(JSON a, JSON b, bool force)
        {
            if (a == null || b == null) return;
            if (a._opt == null) a._opt = new Dictionary<string, object>();
            if (b._opt == null) b._opt = new Dictionary<string, object>();
            foreach (var c in a._opt) if (force || !b._opt.ContainsKey(c.Key)) b.SetProperty(c.Key, c.Value);
        }
        #endregion;
    }
}
