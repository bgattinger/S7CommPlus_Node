using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace S7CommPlusGUIBrowser
{
    public partial class Form1 : Form
    {

        private S7CommPlusConnection conn = null;

        public Form1()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            // 1st argument can be the plc ip-address, otherwise use default
            if (args.Length >= 2)
            {
                tbIpAddress.Text = args[1];
            }
            // 2nd argument can be the plc password, otherwise use default (no password)
            if (args.Length >= 3)
            {
                tbPassword.Text = args[2];
            }
        }

        private void setStatus(string status)
        {
            lbStatus.Text = status;
            lbStatus.Refresh();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            setStatus("connecting...");

            if (conn != null) conn.Disconnect();
            conn = new S7CommPlusConnection();
            int res = conn.Connect(tbIpAddress.Text, tbPassword.Text);
            if (res != 0)
            {
                setStatus("error");
                return;
            }
            setStatus("connected");

            treeView1.Nodes.Clear();
            setStatus("loading...");
            List<S7CommPlusConnection.DatablockInfo> dbInfoList;
            res = conn.GetListOfDatablocks(out dbInfoList);
            if (res != 0)
            {
                setStatus("error");
                return;
            }
            TreeNode tn;
            foreach (S7CommPlusConnection.DatablockInfo dbInfo in dbInfoList)
            {
                tn = treeView1.Nodes.Add(dbInfo.db_name);
                tn.Nodes.Add("Loading...");
                tn.Tag = dbInfo.db_block_ti_relid;
                tn.ImageKey = "Datablock";
                tn.SelectedImageKey = tn.ImageKey;
            }
            // Inputs
            tn = treeView1.Nodes.Add("Inputs");
            tn.Nodes.Add("Loading...");
            tn.Tag = 0x90010000;
            tn.ImageKey = "Default";
            tn.SelectedImageKey = tn.ImageKey;
            // Outputs
            tn = treeView1.Nodes.Add("Outputs");
            tn.Nodes.Add("Loading...");
            tn.Tag = 0x90020000;
            tn.ImageKey = "Default";
            tn.SelectedImageKey = tn.ImageKey;
            // Merker
            tn = treeView1.Nodes.Add("Merker");
            tn.Nodes.Add("Loading...");
            tn.Tag = 0x90030000;
            tn.ImageKey = "Default";
            tn.SelectedImageKey = tn.ImageKey;
            // S7Timers
            tn = treeView1.Nodes.Add("S7Timers");
            tn.Nodes.Add("Loading...");
            tn.Tag = 0x90050000;
            tn.ImageKey = "Default";
            tn.SelectedImageKey = tn.ImageKey;
            // S7Counters
            tn = treeView1.Nodes.Add("S7Counters");
            tn.Nodes.Add("Loading...");
            tn.Tag = 0x90060000;
            tn.ImageKey = "Default";
            tn.SelectedImageKey = tn.ImageKey;

            setStatus("connected");
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            setStatus("disconnecting...");

            if (conn != null) conn.Disconnect();
            conn = null;
            treeView1.Nodes.Clear();

            setStatus("disconnected");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (conn != null) conn.Disconnect();
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count < 0 || e.Node.Nodes[0].Text != "Loading...") return;

            setStatus("loading...");
            e.Node.Nodes.Clear();
            uint relTiId = (uint)e.Node.Tag;
            PObject pObj = conn.getTypeInfoByRelId(relTiId);
            setStatus("connected");

            if (pObj == null || pObj.VarnameList == null) return;
            TreeNode tn;
            TreeNode tnarr;
            for (int i = 0; i < pObj.VarnameList.Names.Count; ++i)
            {
                tn = e.Node.Nodes.Add(pObj.VarnameList.Names[i]);
                SetImageKey(ref tn, pObj.VartypeList.Elements[i]);
                if (pObj.VartypeList.Elements[i].OffsetInfoType.Is1Dim())
                {
                    var ioitarr = (IOffsetInfoType_1Dim)pObj.VartypeList.Elements[i].OffsetInfoType;
                    uint arrayElementCount = ioitarr.GetArrayElementCount();
                    int arrayLowerBounds = ioitarr.GetArrayLowerBounds();
                    for (int j = 0; j < arrayElementCount; ++j)
                    {
                        tnarr = tn.Nodes.Add(pObj.VarnameList.Names[i] + "[" + (j + arrayLowerBounds) + "]");
                        SetImageKey(ref tnarr, pObj.VartypeList.Elements[i]);
                        if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation())
                        {
                            var ioit = (IOffsetInfoType_Relation)pObj.VartypeList.Elements[i].OffsetInfoType;
                            tnarr.Nodes.Add("Loading...");
                            tnarr.Tag = ioit.GetRelationId();
                            SetImageKey(ref tnarr, pObj.VartypeList.Elements[i]);
                        }
                    }
                    tn.Tag = (uint)0; // is array
                }
                else if (pObj.VartypeList.Elements[i].OffsetInfoType.IsMDim())
                {
                    var ioitarrm = (IOffsetInfoType_MDim)pObj.VartypeList.Elements[i].OffsetInfoType;
                    uint[] MdimArrayElementCount = ioitarrm.GetMdimArrayElementCount();
                    int[] MdimArrayLowerBounds = ioitarrm.GetMdimArrayLowerBounds();
                    int dimCount = MdimArrayElementCount.Aggregate(0, (acc, act) => acc += (act > 0) ? 1 : 0);
                    int[] indexes = new int[dimCount];
                    bool stop = false;
                    while (!stop)
                    {
                        string arrIdxStr = "";
                        for (int j = dimCount - 1; j >= 0; --j)
                        {
                            arrIdxStr += (arrIdxStr != "" ? "," : "") + (indexes[j] + MdimArrayLowerBounds[j]);
                        }
                        tnarr = tn.Nodes.Add(pObj.VarnameList.Names[i] + "[" + arrIdxStr + "]");
                        SetImageKey(ref tnarr, pObj.VartypeList.Elements[i]);
                        if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation())
                        {
                            var ioit = (IOffsetInfoType_Relation)pObj.VartypeList.Elements[i].OffsetInfoType;
                            tnarr.Nodes.Add("Loading...");
                            tnarr.Tag = ioit.GetRelationId();
                            SetImageKey(ref tnarr, pObj.VartypeList.Elements[i]);
                        }
                        ++indexes[0];
                        for (int j = 0; j < dimCount; ++j)
                        {
                            if (indexes[j] >= MdimArrayElementCount[j])
                            {
                                if (j + 1 < dimCount)
                                {
                                    indexes[j] = 0;
                                    ++indexes[j + 1];
                                }
                                else
                                {
                                    stop = true;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    tn.Tag = (uint)0; // is array
                }
                else
                {
                    if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation())
                    {
                        var ioit = (IOffsetInfoType_Relation)pObj.VartypeList.Elements[i].OffsetInfoType;
                        tn.Nodes.Add("Loading...");
                        tn.Tag = ioit.GetRelationId();
                        SetImageKey(ref tn, pObj.VartypeList.Elements[i]);
                    }
                }
            }
        }

        private void SetImageKey(ref TreeNode tn, PVartypeListElement vte)
        {
            string sk = "Tag";
            switch (vte.Softdatatype)
            {
                case Softdatatype.S7COMMP_SOFTDATATYPE_BOOL:
                case Softdatatype.S7COMMP_SOFTDATATYPE_BBOOL:
                    sk = "Boolean";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_INT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_ULINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_USINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UDINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_SINT:
                // Derived types
                case Softdatatype.S7COMMP_SOFTDATATYPE_BLOCKFB:
                case Softdatatype.S7COMMP_SOFTDATATYPE_BLOCKFC:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBDELAY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBTOD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBCYCLIC:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBATT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_PORT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_RTM:
                case Softdatatype.S7COMMP_SOFTDATATYPE_PIP:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBPCYCLE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBHWINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBDIAG:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBTIMEERROR:
                case Softdatatype.S7COMMP_SOFTDATATYPE_OBSTARTUP:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DBANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DBWWW:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DBDYN:
                    sk = "Integer2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_BYTE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DWORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LWORD:
                // Derived types
                case Softdatatype.S7COMMP_SOFTDATATYPE_AOMIDENT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTATT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_AOMAID:
                case Softdatatype.S7COMMP_SOFTDATATYPE_AOMLINK:
                case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTHWINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWIOSYSTEM:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWDPMASTER:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWDEVICE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWDPSLAVE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWIO:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWMODULE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWSUBMODULE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWHSC:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWPWM:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWPTO:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWINTERFACE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_HWIEPORT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_CONNANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_CONNPRG:
                case Softdatatype.S7COMMP_SOFTDATATYPE_CONNOUC:
                case Softdatatype.S7COMMP_SOFTDATATYPE_CONNRID:
                    sk = "Binary2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_REAL:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LREAL:
                    sk = "Number2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_CHAR:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WCHAR:
                    sk = "Char";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_STRING:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WSTRING:
                    sk = "Text";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATE:
                    sk = "Date";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTOD:
                    sk = "Time";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIMER:
                    sk = "Timer"; // Duration
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LDT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DTL:
                    sk = "DateTime";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_ANY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_POINTER:
                case Softdatatype.S7COMMP_SOFTDATATYPE_REMOTE:
                    sk = "Any";
                    break;
            }
            if (vte.OffsetInfoType.HasRelation())
            {
                sk = "Structure";
            }
            tn.ImageKey = sk;
            tn.SelectedImageKey = tn.ImageKey;
        }

        private string escapeTiaString(string str, bool isRootNode, bool isArray)
        {
            if (isRootNode) return '"' + str + '"';
            Regex re = new Regex("(^[0-9]|[^0-9A-Za-z_])");
            if (isArray)
            {
                Regex reArr = new Regex("^([^\"]*)(\\[[0-9, ]+\\])$");
                Match m = reArr.Match(str);
                if (!m.Success) return str;
                if (re.Match(m.Groups[1].Value).Success) return '"' + m.Groups[1].Value + '"' + m.Groups[2].Value;
                return str;
            }
            if (re.IsMatch(str)) return '"' + str + '"';
            return str;
        }

        private void readTagBySymbol()
        {
            tbValue.Text = "";
            tbSymbolicAddress.Text = "";

            setStatus("loading...");
            PlcTag tag = conn.getPlcTagBySymbol(tbSymbol.Text);
            setStatus("connected");
            if (tag == null) return;

            tbSymbolicAddress.Text = tag.Address.GetAccessString();

            PlcTags tags = new PlcTags();

            

            tags.AddTag(tag);            
            if (tags.ReadTags(conn) != 0) return;
            tbValue.Text = tag.ToString();
        }

        private void writeTagBySymbol()
        {
            tb_WriteResult.Text = "";
            //tbSymbolicAddress.Text = "";

            setStatus("loading...");
            PlcTag tag = conn.getPlcTagBySymbol(tbSymbol.Text);
            setStatus("connected");
            if (tag == null) return;

            string name = tag.Name;
            ItemAddress address = tag.Address;
            uint softdatatype = tag.Datatype;
            

            tbSymbolicAddress.Text = tag.Address.GetAccessString();            

            PlcTags tags = new PlcTags();


            switch (softdatatype)
            {
                case Softdatatype.S7COMMP_SOFTDATATYPE_BOOL:
                    PlcTagBool tag_bool = new PlcTagBool(name, address, softdatatype);

                    tag_bool.Value = Convert.ToBoolean(tbWriteValue.Text);
                    tags.AddTag(tag_bool);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_bool.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_BYTE:
                    PlcTagByte tag_byte = new PlcTagByte(name, address, softdatatype);

                    tag_byte.Value = Convert.ToByte(tbWriteValue.Text);
                    tags.AddTag(tag_byte);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_byte.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_CHAR:
                    PlcTagChar Tag_char = new PlcTagChar(name, address, softdatatype);

                    Tag_char.Value = Convert.ToChar(tbWriteValue.Text);
                    tags.AddTag(Tag_char);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = Tag_char.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_WORD:
                    PlcTagWord tag_word = new PlcTagWord(name, address, softdatatype);

                    tag_word.Value = Convert.ToUInt16(tbWriteValue.Text);
                    tags.AddTag(tag_word);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_word.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_INT:
                    PlcTagInt tag_int = new PlcTagInt(name, address, softdatatype);

                    tag_int.Value = Convert.ToInt16(tbWriteValue.Text);
                    tags.AddTag(tag_int);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_int.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DWORD:
                    PlcTagDWord tag_dword = new PlcTagDWord(name, address, softdatatype);

                    tag_dword.Value = Convert.ToUInt32(tbWriteValue.Text);
                    tags.AddTag(tag_dword);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_dword.ToString();

                    return;

                case Softdatatype.S7COMMP_SOFTDATATYPE_DINT:
                    PlcTagDInt tag_dint = new PlcTagDInt(name, address, softdatatype);

                    tag_dint.Value = Convert.ToInt32(tbWriteValue.Text);
                    tags.AddTag(tag_dint);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_dint.ToString();

                    return;
                case Softdatatype.S7COMMP_SOFTDATATYPE_REAL:
                    PlcTagReal tag_real = new PlcTagReal(name, address, softdatatype);

                    tag_real.Value = (float) Convert.ToDouble(tbWriteValue.Text);
                    tags.AddTag(tag_real);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_real.ToString();

                    return;
                //case Softdatatype.S7COMMP_SOFTDATATYPE_DATE:
                //    return new PlcTagDate(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY:
                //    return new PlcTagTimeOfDay(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_TIME:
                //    return new PlcTagTime(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME:
                //    return new PlcTagS5Time(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME:
                //    return new PlcTagDateAndTime(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_STRING:
                //    return new PlcTagString(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_POINTER:
                //    return new PlcTagPointer(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_ANY:
                //    return new PlcTagAny(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_BLOCKFB:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_BLOCKFC:
                //    return new PlcTagUInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_COUNTER:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_TIMER:
                //    return new PlcTagUInt(name, address, softdatatype);

                case Softdatatype.S7COMMP_SOFTDATATYPE_BBOOL:
                    //    return new PlcTagBool(name, address, softdatatype);
                    PlcTagBool tag_bbool = new PlcTagBool(name, address, softdatatype);

                    tag_bbool.Value = Convert.ToBoolean(tbWriteValue.Text);
                    tags.AddTag(tag_bbool);

                    if (tags.WriteTags(conn) != 0) return;
                    tbValue.Text = tag_bbool.ToString();

                    return;

                //case Softdatatype.S7COMMP_SOFTDATATYPE_LREAL:
                //    return new PlcTagLReal(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_ULINT:
                //    return new PlcTagULInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_LINT:
                //    return new PlcTagLInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_LWORD:
                //    return new PlcTagLWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_USINT:
                //    return new PlcTagUSInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_UINT:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_UDINT:
                //    return new PlcTagUDInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_SINT:
                //    return new PlcTagSInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_WCHAR:
                //    return new PlcTagWChar(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_WSTRING:
                //    return new PlcTagWString(name, address, softdatatype);
                ////case Softdatatype.S7COMMP_SOFTDATATYPE_VARIANT:
                ////-> Variant isn't added inside of the instance-db as a variable!
                //case Softdatatype.S7COMMP_SOFTDATATYPE_LTIME:
                //    return new PlcTagLTime(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_LTOD:
                //    return new PlcTagLTOD(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_LDT:
                //    return new PlcTagLDT(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_DTL:
                //    return new PlcTagDTL(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_REMOTE:
                //    return new PlcTagAny(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_AOMIDENT:
                //    return new PlcTagDWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTANY:
                //    return new PlcTagDWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTATT:
                //    return new PlcTagDWord(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_AOMAID:
                //    return new PlcTagDWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_AOMLINK:
                //    return new PlcTagDWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_EVENTHWINT:
                //    return new PlcTagDWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWANY:
                //    return new PlcTagWord(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWIOSYSTEM:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWDPMASTER:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWDEVICE:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWDPSLAVE:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWIO:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWMODULE:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWSUBMODULE:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWHSC:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWPWM:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWPTO:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWINTERFACE:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_HWIEPORT:
                //    return new PlcTagWord(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBANY:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBDELAY:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBTOD:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBCYCLIC:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBATT:
                //    return new PlcTagInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_CONNANY:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_CONNPRG:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_CONNOUC:
                //    return new PlcTagWord(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_CONNRID:
                //    return new PlcTagDWord(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_PORT:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_RTM:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_PIP:
                //    return new PlcTagUInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBPCYCLE:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBHWINT:
                //    return new PlcTagInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBDIAG:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBTIMEERROR:
                //    return new PlcTagInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_OBSTARTUP:
                //    return new PlcTagInt(name, address, softdatatype);

                //case Softdatatype.S7COMMP_SOFTDATATYPE_DBANY:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_DBWWW:
                //    return new PlcTagUInt(name, address, softdatatype);
                //case Softdatatype.S7COMMP_SOFTDATATYPE_DBDYN:
                //    return new PlcTagUInt(name, address, softdatatype);

                default:
                    Console.WriteLine("ERROR: Unknown softdatatype=" + softdatatype.ToString() + " for variable= " + name);
                    return;
                    
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null) return; // has relId

            string name = "";
            TreeNode tn = e.Node;
            while (tn != null)
            {
                bool isArray = false;
                string nodeText = tn.Text;
                tn = tn.Parent;
                if (tn != null && tn.Tag != null)
                { // is array
                    if ((uint)tn.Tag == 0)
                    {
                        isArray = true;
                        tn = tn.Parent; // skip array parent
                    }
                }
                if (tn != null && tn.Tag != null)
                { // don't add in/out/merker area as tag
                    uint relId = (uint)tn.Tag;
                    if (relId == 0x90010000 || relId == 0x90020000 || relId == 0x90030000) tn = null;
                }
                name = escapeTiaString(nodeText, tn == null, isArray) + (name != "" ? "." : "") + name;
            }
            tbSymbol.Text = name;

            readTagBySymbol();
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (tbSymbol.Text == "") return;

            try
            {
                readTagBySymbol();
            }
           
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            if (tbSymbol.Text == "") return;

            try
            {
                writeTagBySymbol();
            }

            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

    }
}
