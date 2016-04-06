using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace TSW
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SpeechInfo
        {
            public Int32 Idx;
            public string Message;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SpeechFile
        {
            public Int32 Count;
            public List<SpeechInfo> Spz;
        }

        [Serializable]
        public class SpeechXML
        {
            public SpeechXML() { }

            [XmlElementAttribute("Idx")]
            public Int32 IdxNum { get; set; }


            [XmlIgnore]
            public string Message { get; set; }
            [XmlElement("Message")]
            public System.Xml.XmlCDataSection MessageCDATA
            {
                get
                {
                    return new System.Xml.XmlDocument().CreateCDataSection(Message);
                }
                set
                {
                    Message = value.Value;
                }
            }
        }

        [Serializable]
        public class SpeechRoot
        {
            [XmlElementAttribute("Count")]
            public Int32 CountNum { get; set; }

            [XmlArrayItem(typeof(SpeechXML))]
            public SpeechXML[] Spz { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        public bool Read<T>(byte[] buffer, int index, ref T retval)
        {
            if (index == buffer.Length) return false;
            int size = Marshal.SizeOf(typeof(T));
            if (index + size > buffer.Length) throw new IndexOutOfRangeException();
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr addr = (IntPtr)((long)handle.AddrOfPinnedObject() + index);
                retval = (T)Marshal.PtrToStructure(addr, typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return true;
        }

        public bool Read<T>(Stream stream, ref T retval)
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = null;
            if (buffer == null || size > buffer.Length) buffer = new byte[size];
            int len = stream.Read(buffer, 0, size);
            if (len == 0) return false;
            if (len != size) throw new EndOfStreamException();
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                retval = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return true;
        }

        public bool ReadBytes(Stream stream, UInt32 readSize, ref Byte[] bArray)
        {
            int size = (int)readSize;
            int len = stream.Read(bArray, 0, size);
            if (len == 0) return false;
            if (len != size) throw new EndOfStreamException();
            return true;
        }

        public Int16 ReadInt16(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            return br.ReadInt16();
        }

        public Int32 ReadInt32(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            return br.ReadInt32();
        }


        public void ReadAndSetByType(FieldInfo field, TypedReference tr, BinaryReader br, Stream _stream)
        {
            if (!field.FieldType.IsArray)
            {
                if (field.FieldType == typeof(Int32))
                {
                    field.SetValueDirect(tr, br.ReadInt32());
                }
                else if (field.FieldType == typeof(string))
                {
                    Int32 _count = br.ReadInt16();
                    _count *= 2;
                    Byte[] _data = br.ReadBytes(_count);
                    string _asciistring = UnicodeEncoding.Unicode.GetString(_data);
                    field.SetValueDirect(tr, _asciistring);
                }
            }
        }

        public T ReadStreamStruct<T>(Stream _stream)
        {
            BinaryReader br = new BinaryReader(_stream);
            T _ObjStruct = (T)Activator.CreateInstance(typeof(T));
            TypedReference tr = __makeref(_ObjStruct);
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ReadAndSetByType(field, tr, br, _stream);
            }
            return _ObjStruct;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog _openDlg = new OpenFileDialog();
            _openDlg.Filter = "RES|*.Res|All Files (*.*)|*.*";
            _openDlg.FilterIndex = 1;
            DialogResult result = _openDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                string _directory = Path.GetDirectoryName(_openDlg.FileName);
                string _fname = Path.GetFileNameWithoutExtension(_openDlg.FileName);
                Stream _datsource = new FileStream(_openDlg.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryReader br = new BinaryReader(_datsource);
                SpeechFile spf = new SpeechFile();
                spf.Spz = new List<SpeechInfo>();
                spf.Count = br.ReadInt32();
                for(int i = 0; i < spf.Count; ++i)
                {
                    SpeechInfo spi = ReadStreamStruct<SpeechInfo>(_datsource);
                    spf.Spz.Add(spi);
                }

                string write_path = _directory + "\\" + _fname + ".xml";

                SpeechRoot sproot = new SpeechRoot();
                sproot.CountNum = spf.Count;
                sproot.Spz = new SpeechXML[sproot.CountNum];
                for (int i = 0; i < spf.Count; ++i)
                {
                    SpeechInfo spi = spf.Spz[i];
                    sproot.Spz[i] = new SpeechXML();
                    sproot.Spz[i].IdxNum = spi.Idx;
                    sproot.Spz[i].Message = spi.Message;
                }

                System.Xml.Serialization.XmlSerializer writer =  new System.Xml.Serialization.XmlSerializer(typeof(SpeechRoot));
                System.IO.StreamWriter file = new System.IO.StreamWriter(new System.IO.FileStream(write_path, System.IO.FileMode.Create), Encoding.Unicode);
                writer.Serialize(file, sproot);
                file.Close();
                _datsource.Close();

            }
       }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog _openDlg = new OpenFileDialog();
            _openDlg.Filter = "XML|*.xml|All Files (*.*)|*.*";
            _openDlg.FilterIndex = 1;
            DialogResult result = _openDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                string _directory = Path.GetDirectoryName(_openDlg.FileName);
                string _fname = Path.GetFileNameWithoutExtension(_openDlg.FileName);
                SpeechRoot sproot = null;

                XmlSerializer serializer = new XmlSerializer(typeof(SpeechRoot));

                StreamReader reader = new StreamReader(_openDlg.FileName);
                sproot = (SpeechRoot)serializer.Deserialize(reader);
                reader.Close();

                string write_path = _directory  + _fname + "_new.res";
                Stream _res = new FileStream(write_path, FileMode.Create, FileAccess.Write, FileShare.Write);
                BinaryWriter bw = new BinaryWriter(_res);
                bw.Write(sproot.CountNum);
                for (int i = 0; i < sproot.CountNum; ++i)
                {
                    SpeechXML spi = sproot.Spz[i];
                    bw.Write(sproot.Spz[i].IdxNum);
                    bw.Write((Int16)(sproot.Spz[i].Message.Length));
                    bw.Write(Encoding.Unicode.GetBytes(sproot.Spz[i].Message));
                }
                string mg5 = "1b0adc69332dc56adcda5f4fea4c3103";
                bw.Write((Int16)(32));
                bw.Write(Encoding.ASCII.GetBytes(mg5));
                _res.Close();
            }
         }
    }
}
