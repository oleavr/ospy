using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using oSpy.SharpDumpLib;
using System.Xml;

namespace oSpy.Playground
{
    enum KSPROPERTY_PIN
    {
        CINSTANCES,
        CTYPES,
        DATAFLOW,
        DATARANGES,
        DATAINTERSECTION,
        INTERFACES,
        MEDIUMS,
        COMMUNICATION,
        GLOBALCINSTANCES,
        NECESSARYINSTANCES,
        PHYSICALCONNECTION,
        CATEGORY,
        NAME,
        CONSTRAINEDDATARANGES,
        PROPOSEDATAFORMAT
    }

    enum KSPROPERTY_CONNECTION
    {
        STATE,
        PRIORITY,
        DATAFORMAT,
        ALLOCATORFRAMING,
        PROPOSEDATAFORMAT,
        ACQUIREORDERING,
        ALLOCATORFRAMING_EX,
        STARTAT
    }

    enum KSPROPERTY_ALLOCATOR_CONTROL
    {
        HONOR_COUNT,
        SURFACE_SIZE,
        CAPTURE_CAPS,
        CAPTURE_INTERLEAVE
    }

    class KsPropertySet
    {
        private string name;
        public string Name
        {
            get { return name; }
        }

        private Type enumType;
        public Type EnumType
        {
            get { return enumType; }
        }

        public KsPropertySet (string name, Type enumType)
        {
            this.name = name;
            this.enumType = enumType;
        }
    }

    public partial class VisualizationForm : Form
    {
        private Dump dump;
        MultiSessionView view;

        Dictionary<Guid, object> ksPropertySets;
        List<KeyValuePair<uint, string>> ksPropertyFlags;

        public VisualizationForm (Dump dump)
        {
            InitializeComponent ();

            this.dump = dump;
            view = new MultiSessionView ();
            view.Parent = this;
            view.Dock = DockStyle.Fill;

            // Property sets
            ksPropertySets = new Dictionary<Guid, object> ();

            // ks.h
            ksPropertySets[new Guid ("{1464EDA5-6A8F-11D1-9AA7-00A0C9223196}")] = "KSPROPSETID_General";
            ksPropertySets[new Guid ("{EE904F0C-D09B-11D0-ABE9-00A0C9223196}")] = "KSPROPSETID_MediaSeeking";
            ksPropertySets[new Guid ("{720D4AC0-7533-11D0-A5D6-28DB04C10000}")] = "KSPROPSETID_Topology";
            ksPropertySets[new Guid ("{AF627536-E719-11D2-8A1D-006097D2DF5D}")] = "KSPROPSETID_GM";
            ksPropertySets[new Guid ("{8C134960-51AD-11CF-878A-94F801C10000}")] = new KsPropertySet ("KSPROPSETID_Pin", typeof (KSPROPERTY_PIN));
            ksPropertySets[new Guid ("{D16AD380-AC1A-11CF-A5D6-28DB04C10000}")] = "KSPROPSETID_Quality";
            ksPropertySets[new Guid ("{1D58C920-AC9B-11CF-A5D6-28DB04C10000}")] = new KsPropertySet ("KSPROPSETID_Connection", typeof (KSPROPERTY_CONNECTION));
            ksPropertySets[new Guid ("{0A3D1C5D-5243-4819-9ED0-AEE8044CEE2B}")] = "KSPROPSETID_MemoryTransport";
            ksPropertySets[new Guid ("{CF6E4342-EC87-11CF-A130-0020AFD156E4}")] = "KSPROPSETID_StreamAllocator";
            ksPropertySets[new Guid ("{1FDD8EE1-9CD3-11D0-82AA-0000F822FE8A}")] = "KSPROPSETID_StreamInterface";
            ksPropertySets[new Guid ("{65AABA60-98AE-11CF-A10D-0020AFD156E4}")] = "KSPROPSETID_Stream";
            ksPropertySets[new Guid ("{DF12A4C0-AC17-11CF-A5D6-28DB04C10000}")] = "KSPROPSETID_Clock";

            // KsMedia.h
            ksPropertySets[new Guid ("{437B3414-D060-11D0-8583-00C04FD9BAF3}")] = "KSPROPSETID_DirectSound3DListener";
            ksPropertySets[new Guid ("{437B3411-D060-11D0-8583-00C04FD9BAF3}")] = "KSPROPSETID_DirectSound3DBuffer";
            ksPropertySets[new Guid ("{B66DECB0-A083-11D0-851E-00C04FD9BAF3}")] = "KSPROPSETID_Hrtf3d";
            ksPropertySets[new Guid ("{6429F090-9FD9-11D0-A75B-00A0C90365E3}")] = "KSPROPSETID_Itd3d";
            ksPropertySets[new Guid ("{07BA150E-E2B1-11D0-AC17-00A0C9223196}")] = "KSPROPSETID_Bibliographic";
            ksPropertySets[new Guid ("{45FFAAA1-6E1B-11D0-BCF2-444553540000}")] = "KSPROPSETID_TopologyNode";
            ksPropertySets[new Guid ("{A855A48C-2F78-4729-9051-1968746B9EEF}")] = "KSPROPSETID_RtAudio";
            ksPropertySets[new Guid ("{2F2C8DDD-4198-4FAC-BA29-61BB05B7DE06}")] = "KSPROPSETID_DrmAudioStream";
            ksPropertySets[new Guid ("{45FFAAA0-6E1B-11D0-BCF2-444553540000}")] = "KSPROPSETID_Audio";
            ksPropertySets[new Guid ("{D7A4AF8B-3DC1-4902-91EA-8A15C90E05B2}")] = "KSPROPSETID_Acoustic_Echo_Cancel";
            ksPropertySets[new Guid ("{16A15B10-16F0-11D0-A195-0020AFD156E4}")] = "KSPROPSETID_Wave_Queued";
            ksPropertySets[new Guid ("{924E54B0-630F-11CF-ADA7-08003E30494A}")] = "KSPROPSETID_Wave";
            ksPropertySets[new Guid ("{8539E660-62E9-11CF-A5D6-28DB04C10000}")] = "KSPROPSETID_WaveTable";
            ksPropertySets[new Guid ("{3FFEAEA0-2BEE-11CF-A5D6-28DB04C10000}")] = "KSPROPSETID_Cyclic";
            ksPropertySets[new Guid ("{CBE3FAA0-CC75-11D0-B465-00001A1818E6}")] = "KSPROPSETID_Sysaudio";
            ksPropertySets[new Guid ("{A3A53220-C6E4-11D0-B465-00001A1818E6}")] = "KSPROPSETID_Sysaudio_Pin";
            ksPropertySets[new Guid ("{79A9312E-59AE-43B0-A350-8B05284CAB24}")] = "KSPROPSETID_AudioGfx";
            ksPropertySets[new Guid ("{5A2FFE80-16B9-11D0-A5D6-28DB04C10000}")] = "KSPROPSETID_Linear";
            ksPropertySets[new Guid ("{BFABE720-6E1F-11D0-BCF2-444553540000}")] = "KSPROPSETID_AC3";
            ksPropertySets[new Guid ("{6CA6E020-43BD-11D0-BD6A-003505C103A9}")] = "KSPROPSETID_AudioDecoderOut";
            ksPropertySets[new Guid ("{AC390460-43AF-11D0-BD6A-003505C103A9}")] = "KSPROPSETID_DvdSubPic";
            ksPropertySets[new Guid ("{0E8A0A40-6AEF-11D0-9ED0-00A024CA19B3}")] = "KSPROPSETID_CopyProt";
            ksPropertySets[new Guid ("{F162C607-7B35-496F-AD7F-2DCA3B46B718}")] = "KSPROPSETID_VBICAP_PROPERTIES";
            ksPropertySets[new Guid ("{CAFEB0CA-8715-11D0-BD6A-0035C0EDBABE}")] = "KSPROPSETID_VBICodecFiltering";
            ksPropertySets[new Guid ("{E73FACE3-2880-4902-B799-88D0CD634E0F}")] = "KSPROPSETID_VramCapture";
            ksPropertySets[new Guid ("{490EA5CF-7681-11D1-A21C-00A0C9223196}")] = "KSPROPSETID_OverlayUpdate";
            ksPropertySets[new Guid ("{BC29A660-30E3-11D0-9E69-00C04FD7C15B}")] = "KSPROPSETID_VPConfig";
            ksPropertySets[new Guid ("{EC529B00-1A1F-11D1-BAD9-00609744111A}")] = "KSPROPSETID_VPVBIConfig";
            ksPropertySets[new Guid ("{A503C5C0-1D1D-11D1-AD80-444553540000}")] = "KSPROPSETID_TSRateChange";
            ksPropertySets[new Guid ("{4509F757-2D46-4637-8E62-CE7DB944F57B}")] = "KSPROPSETID_Jack";

            ksPropertySets[new Guid ("{53171960-148E-11D2-9979-0000C0CC16BA}")] = new KsPropertySet ("PROPSETID_ALLOCATOR_CONTROL", typeof (KSPROPERTY_ALLOCATOR_CONTROL));
            ksPropertySets[new Guid ("{C6E13360-30AC-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_VIDEOPROCAMP";
            ksPropertySets[new Guid ("{1ABDAECA-68B6-4F83-9371-B413907C7B9F}")] = "PROPSETID_VIDCAP_SELECTOR";
            ksPropertySets[new Guid ("{6A2E0605-28E4-11D0-A18C-00A0C9118956}")] = "PROPSETID_TUNER";
            ksPropertySets[new Guid ("{6A2E0610-28E4-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_VIDEOENCODER";
            ksPropertySets[new Guid ("{C6E13350-30AC-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_VIDEODECODER";
            ksPropertySets[new Guid ("{C6E13370-30AC-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_CAMERACONTROL";
            ksPropertySets[new Guid ("{B5730A90-1A2C-11CF-8C23-00AA006B6814}")] = "PROPSETID_EXT_DEVICE";
            ksPropertySets[new Guid ("{A03CD5F0-3045-11CF-8C44-00AA006B6814}")] = "PROPSETID_EXT_TRANSPORT";
            ksPropertySets[new Guid ("{9B496CE1-811B-11CF-8C77-00AA006B6814}")] = "PROPSETID_TIMECODE_READER";
            ksPropertySets[new Guid ("{6A2E0640-28E4-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_CROSSBAR";
            ksPropertySets[new Guid ("{6A2E0650-28E4-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_TVAUDIO";
            ksPropertySets[new Guid ("{C6E13343-30AC-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_VIDEOCOMPRESSION";
            ksPropertySets[new Guid ("{6A2E0670-28E4-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_VIDEOCONTROL";
            ksPropertySets[new Guid ("{C6E13344-30AC-11D0-A18C-00A0C9118956}")] = "PROPSETID_VIDCAP_DROPPEDFRAMES";

            // Flags
            ksPropertyFlags = new List<KeyValuePair<uint, string>> ();
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000001, "GET"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000002, "SET"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000100, "SETSUPPORT"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000200, "BASICSUPPORT"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000400, "RELATIONS"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00000800, "SERIALIZESET"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00001000, "UNSERIALIZESET"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00002000, "SERIALIZERAW"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00004000, "UNSERIALIZERAW"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00008000, "SERIALIZESIZE"));
            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x00010000, "DEFAULTVALUES"));

            ksPropertyFlags.Add (new KeyValuePair<uint, string> (0x10000000, "TOPOLOGY"));

            VisualizeDump (dump);
        }

        private void VisualizeDump (Dump dump)
        {
            Dictionary<uint, VisualSession> sessions = new Dictionary<uint,VisualSession> ();

            foreach (KeyValuePair<uint, Event> pair in dump.Events)
            {
                Event ev = pair.Value;

                VisualSession session;

                if (sessions.ContainsKey (ev.ThreadId))
                    session = sessions[ev.ThreadId];
                else
                {
                    session = new VisualSession (String.Format ("{0}", ev.ThreadId));
                    sessions[ev.ThreadId] = session;
                }

                VisualTransaction tr = null;

                XmlElement eventRoot = ev.Data;
                XmlNode node = eventRoot.SelectSingleNode ("/event/name");
                if (node != null)
                {
                    string headline = node.InnerText.Trim ().Split (new string[] { "::" }, 2, StringSplitOptions.None)[1];

                    tr = new VisualTransaction (ev.Id, (headline == "DeviceIoControl") ? TransactionDirection.Out : TransactionDirection.In, ev.Timestamp);

                    XmlNode handleNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[1]/value");
                    XmlNode codeNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[2]/value");
                    string handleStr = null, codeStr = null;
                    if (handleNode != null && codeNode != null)
                    {
                        handleStr = handleNode.Attributes["value"].Value;
                        codeStr = codeNode.Attributes["value"].Value;
                        headline += String.Format (" ({0}, {1})", handleStr, codeStr);
                    }

                    tr.HeadlineText = headline;

                    XmlNode inBufNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[3]/value/value");
                    byte[] inBuf = null;
                    if (inBufNode != null)
                    {
                        inBuf = Convert.FromBase64String (inBufNode.InnerText);

                        if (codeStr == "IOCTL_KS_PROPERTY")
                        {
                            int offset = 0;

                            byte[] rawSetGuid = new byte[16];
                            Array.Copy (inBuf, rawSetGuid, rawSetGuid.Length);
                            Guid propSetGuid = new Guid (rawSetGuid);
                            offset += rawSetGuid.Length;

                            uint rawPropId = BitConverter.ToUInt32 (inBuf, offset);
                            offset += 4;

                            uint propFlags = BitConverter.ToUInt32 (inBuf, offset);
                            offset += 4;

                            string propSetStr, propIdStr, propFlagsStr;
                            string payloadStr = null;

                            if (ksPropertySets.ContainsKey (propSetGuid))
                            {
                                object o = ksPropertySets[propSetGuid];

                                if (o is string)
                                {
                                    propSetStr = o as string;
                                    propIdStr = String.Format ("0x{0:x8}", rawPropId);
                                }
                                else
                                {
                                    KsPropertySet propSet = o as KsPropertySet;
                                    propSetStr = propSet.Name;
                                    propIdStr = Enum.GetName (propSet.EnumType, rawPropId);
                                }
                            }
                            else
                            {
                                propSetStr = propSetGuid.ToString ("B");
                                propIdStr = String.Format ("0x{0:x8}", rawPropId);
                            }

                            propFlagsStr = "";
                            foreach (KeyValuePair<uint, string> flagPair in ksPropertyFlags)
                            {
                                uint mask = flagPair.Key;
                                string name = flagPair.Value;

                                if ((propFlags & mask) != 0)
                                {
                                    if (propFlagsStr.Length > 0)
                                        propFlagsStr += "|";
                                    propFlagsStr += name;
                                }
                            }

                            int payloadSize = inBuf.Length - offset;
                            if (payloadSize > 0)
                            {
                                byte[] payload = new byte[payloadSize];
                                Array.Copy (inBuf, offset, payload, 0, payload.Length);
                                payloadStr = StaticUtils.ByteArrayToHexDump (payload);
                            }

                            string body = String.Format ("{0}, {1}, {2}", propSetStr, propIdStr, propFlagsStr);
                            if (payloadStr != null)
                            {
                                body += "\r\n\r\n" + payloadStr;
                            }

                            tr.BodyText = body;
                        }
                        else
                        {
                            tr.SetBodyFromPreviewData (inBuf, inBuf.Length);
                        }
                    }
                }
                else
                {
                    tr = new VisualTransaction (ev.Id, ev.Timestamp);
                    tr.HeadlineText = String.Format ("<{0}>", ev.Type);
                }

                session.Transactions.Add (tr);
            }

            VisualSession[] sessionsArray = new VisualSession[sessions.Count];
            sessions.Values.CopyTo (sessionsArray, 0);
            view.Sessions = sessionsArray;
        }
    }
}
