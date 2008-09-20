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
    enum KSPROPERTY_MEDIASEEKING
    {
        CAPABILITIES,
        FORMATS,
        TIMEFORMAT,
        POSITION,
        STOPPOSITION,
        POSITIONS,
        DURATION,
        AVAILABLE,
        PREROLL,
        CONVERTTIMEFORMAT
    }

    enum KSPROPERTY_TOPOLOGY
    {
        CATEGORIES,
        NODES,
        CONNECTIONS,
        NAME
    }

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

    enum KSPROPERTY_QUALITY
    {
        REPORT,
        ERROR
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

    enum KSPROPERTY_MEMORY_TRANSPORT
    {
        MEMORY_TRANSPORT = 1
    }

    enum KSPROPERTY_STREAM
    {
        ALLOCATOR,
        QUALITY,
        DEGRADATION,
        MASTERCLOCK,
        TIMEFORMAT,
        PRESENTATIONTIME,
        PRESENTATIONEXTENT,
        FRAMETIME,
        RATECAPABILITY,
        RATE,
        PIPE_ID
    }

    enum KSPROPERTY_CLOCK
    {
        TIME,
        PHYSICALTIME,
        CORRELATEDTIME,
        CORRELATEDPHYSICALTIME,
        RESOLUTION,
        STATE,
        FUNCTIONTABLE
    }

    enum KSPROPERTY_VIDMEM_TRANSPORT
    {
        DISPLAY_ADAPTER_GUID = 1,
        PREFERRED_CAPTURE_SURFACE,
        CURRENT_CAPTURE_SURFACE,
        MAP_CAPTURE_HANDLE_TO_VRAM_ADDRESS
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

        Dictionary<uint, string> errorCodes = new Dictionary<uint, string> ();

        Dictionary<Guid, object> ksPropertySets = new Dictionary<Guid,object> ();
        List<KeyValuePair<uint, string>> ksPropertyFlags = new List<KeyValuePair<uint, string>> ();

        Dictionary<Guid, string> ksCategories = new Dictionary<Guid, string> ();
        Dictionary<Guid, string> ksMajorFormats = new Dictionary<Guid,string> ();
        Dictionary<Guid, string> ksSubFormats = new Dictionary<Guid,string> ();
        Dictionary<Guid, string> ksSpecifiers = new Dictionary<Guid,string> ();

        Dictionary<Guid, string> ksMemoryTypes = new Dictionary<Guid, string> ();
        Dictionary<Guid, string> ksBusTypes = new Dictionary<Guid, string> ();
        List<KeyValuePair<uint, string>> ksAllocatorCreateFlags = new List<KeyValuePair<uint, string>> ();
        List<KeyValuePair<uint, string>> ksAllocatorQueryFlags = new List<KeyValuePair<uint, string>> ();

        List<KeyValuePair<uint, string>> ksEventFlags = new List<KeyValuePair<uint, string>> ();

        List<KeyValuePair<uint, string>> fileAlignmentFlags = new List<KeyValuePair<uint, string>> ();

        List<string> interestingHandles = new List<string> ();
        Dictionary<uint, bool> pendingReadStreamRequests = new Dictionary<uint, bool> ();

        public VisualizationForm (Dump dump)
        {
            InitializeComponent ();

            this.dump = dump;
            view = new MultiSessionView ();
            view.Parent = this;
            view.Dock = DockStyle.Fill;

            // Error codes
            errorCodes[31] = "ERROR_GEN_FAILURE";
            errorCodes[234] = "ERROR_MORE_DATA";
            errorCodes[995] = "ERROR_OPERATION_ABORTED";
            errorCodes[997] = "ERROR_IO_PENDING";
            errorCodes[1168] = "ERROR_NOT_FOUND";
            errorCodes[1169] = "ERROR_NO_MATCH";
            errorCodes[1170] = "ERROR_SET_NOT_FOUND";

            // Categories
            ksCategories[new Guid ("{085AFF00-62CE-11CF-A5D6-28DB04C10000}")] = "KSCATEGORY_BRIDGE";
            ksCategories[new Guid ("{65E8773D-8F56-11D0-A3B9-00A0C9223196}")] = "KSCATEGORY_CAPTURE";
            ksCategories[new Guid ("{65E8773E-8F56-11D0-A3B9-00A0C9223196}")] = "KSCATEGORY_RENDER";
            ksCategories[new Guid ("{AD809C00-7B88-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_MIXER";
            ksCategories[new Guid ("{0A4252A0-7E70-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_SPLITTER";
            ksCategories[new Guid ("{1E84C900-7E70-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_DATACOMPRESSOR";
            ksCategories[new Guid ("{2721AE20-7E70-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_DATADECOMPRESSOR";
            ksCategories[new Guid ("{2EB07EA0-7E70-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_DATATRANSFORM";
            ksCategories[new Guid ("{CF1DDA2C-9743-11D0-A3EE-00A0C9223196}")] = "KSCATEGORY_COMMUNICATIONSTRANSFORM";
            ksCategories[new Guid ("{CF1DDA2D-9743-11D0-A3EE-00A0C9223196}")] = "KSCATEGORY_INTERFACETRANSFORM";
            ksCategories[new Guid ("{CF1DDA2E-9743-11D0-A3EE-00A0C9223196}")] = "KSCATEGORY_MEDIUMTRANSFORM";
            ksCategories[new Guid ("{760FED5E-9357-11D0-A3CC-00A0C9223196}")] = "KSCATEGORY_FILESYSTEM";
            ksCategories[new Guid ("{53172480-4791-11D0-A5D6-28DB04C10000}")] = "KSCATEGORY_CLOCK";
            ksCategories[new Guid ("{97EBAACA-95BD-11D0-A3EA-00A0C9223196}")] = "KSCATEGORY_PROXY";
            ksCategories[new Guid ("{97EBAACB-95BD-11D0-A3EA-00A0C9223196}")] = "KSCATEGORY_QUALITY";

            ksCategories[new Guid ("{830a44f2-a32d-476b-be97-42845673b35a}")] = "KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR";
            ksCategories[new Guid ("{6994AD04-93EF-11D0-A3CC-00A0C9223196}")] = "KSCATEGORY_AUDIO";
            ksCategories[new Guid ("{6994AD05-93EF-11D0-A3CC-00A0C9223196}")] = "KSCATEGORY_VIDEO";
            ksCategories[new Guid ("{EB115FFC-10C8-4964-831D-6DCB02E6F23F}")] = "KSCATEGORY_REALTIME";
            ksCategories[new Guid ("{6994AD06-93EF-11D0-A3CC-00A0C9223196}")] = "KSCATEGORY_TEXT";
            ksCategories[new Guid ("{67C9CC3C-69C4-11D2-8759-00A0C9223196}")] = "KSCATEGORY_NETWORK";
            ksCategories[new Guid ("{DDA54A40-1E4C-11D1-A050-405705C10000}")] = "KSCATEGORY_TOPOLOGY";
            ksCategories[new Guid ("{3503EAC4-1F26-11D1-8AB0-00A0C9223196}")] = "KSCATEGORY_VIRTUAL";
            ksCategories[new Guid ("{BF963D80-C559-11D0-8A2B-00A0C9255AC1}")] = "KSCATEGORY_ACOUSTIC_ECHO_CANCEL";
            ksCategories[new Guid ("{A7C7A5B1-5AF3-11D1-9CED-00A024BF0407}")] = "KSCATEGORY_SYSAUDIO";
            ksCategories[new Guid ("{3E227E76-690D-11D2-8161-0000F8775BF1}")] = "KSCATEGORY_WDMAUD";
            ksCategories[new Guid ("{9BAF9572-340C-11D3-ABDC-00A0C90AB16F}")] = "KSCATEGORY_AUDIO_GFX";
            ksCategories[new Guid ("{9EA331FA-B91B-45F8-9285-BD2BC77AFCDE}")] = "KSCATEGORY_AUDIO_SPLITTER";
            ksCategories[new Guid ("{FBF6F530-07B9-11D2-A71E-0000F8004788}")] = "KSCATEGORY_AUDIO_DEVICE";
            ksCategories[new Guid ("{D6C5066E-72C1-11D2-9755-0000F8004788}")] = "KSCATEGORY_PREFERRED_WAVEOUT_DEVICE";
            ksCategories[new Guid ("{D6C50671-72C1-11D2-9755-0000F8004788}")] = "KSCATEGORY_PREFERRED_WAVEIN_DEVICE";
            ksCategories[new Guid ("{D6C50674-72C1-11D2-9755-0000F8004788}")] = "KSCATEGORY_PREFERRED_MIDIOUT_DEVICE";
            ksCategories[new Guid ("{47A4FA20-A251-11D1-A050-0000F8004788}")] = "KSCATEGORY_WDMAUD_USE_PIN_NAME";
            ksCategories[new Guid ("{74f3aea8-9768-11d1-8e07-00a0c95ec22e}")] = "KSCATEGORY_ESCALANTE_PLATFORM_DRIVER";
            ksCategories[new Guid ("{a799a800-a46d-11d0-a18c-00a02401dcd4}")] = "KSCATEGORY_TVTUNER";
            ksCategories[new Guid ("{a799a801-a46d-11d0-a18c-00a02401dcd4}")] = "KSCATEGORY_CROSSBAR";
            ksCategories[new Guid ("{a799a802-a46d-11d0-a18c-00a02401dcd4}")] = "KSCATEGORY_TVAUDIO";
            ksCategories[new Guid ("{a799a803-a46d-11d0-a18c-00a02401dcd4}")] = "KSCATEGORY_VPMUX";
            ksCategories[new Guid ("{07dad660-22f1-11d1-a9f4-00c04fbbde8f}")] = "KSCATEGORY_VBICODEC";
            ksCategories[new Guid ("{19689BF6-C384-48fd-AD51-90E58C79F70B}")] = "KSCATEGORY_ENCODER";
            ksCategories[new Guid ("{7A5DE1D3-01A1-452c-B481-4FA2B96271E8}")] = "KSCATEGORY_MULTIPLEXER";

            ksCategories[new Guid ("{FD0A5AF4-B41D-11d2-9C95-00C04F7971E0}")] = "KSCATEGORY_BDA_RECEIVER_COMPONENT";
            ksCategories[new Guid ("{71985F48-1CA1-11d3-9CC8-00C04F7971E0}")] = "KSCATEGORY_BDA_NETWORK_TUNER";
            ksCategories[new Guid ("{71985F49-1CA1-11d3-9CC8-00C04F7971E0}")] = "KSCATEGORY_BDA_NETWORK_EPG";
            ksCategories[new Guid ("{71985F4A-1CA1-11d3-9CC8-00C04F7971E0}")] = "KSCATEGORY_BDA_IP_SINK";
            ksCategories[new Guid ("{71985F4B-1CA1-11d3-9CC8-00C04F7971E0}")] = "KSCATEGORY_BDA_NETWORK_PROVIDER";
            ksCategories[new Guid ("{A2E3074F-6C3D-11d3-B653-00C04F79498E}")] = "KSCATEGORY_BDA_TRANSPORT_INFORMATION";

            // Property sets

            // ks.h
            ksPropertySets[new Guid ("{1464EDA5-6A8F-11D1-9AA7-00A0C9223196}")] = "KSPROPSETID_General";
            ksPropertySets[new Guid ("{EE904F0C-D09B-11D0-ABE9-00A0C9223196}")] = new KsPropertySet ("KSPROPSETID_MediaSeeking", typeof (KSPROPERTY_MEDIASEEKING));
            ksPropertySets[new Guid ("{720D4AC0-7533-11D0-A5D6-28DB04C10000}")] = new KsPropertySet ("KSPROPSETID_Topology", typeof (KSPROPERTY_TOPOLOGY));
            ksPropertySets[new Guid ("{AF627536-E719-11D2-8A1D-006097D2DF5D}")] = "KSPROPSETID_GM";
            ksPropertySets[new Guid ("{8C134960-51AD-11CF-878A-94F801C10000}")] = new KsPropertySet ("KSPROPSETID_Pin", typeof (KSPROPERTY_PIN));
            ksPropertySets[new Guid ("{D16AD380-AC1A-11CF-A5D6-28DB04C10000}")] = new KsPropertySet ("KSPROPSETID_Quality", typeof (KSPROPERTY_QUALITY));
            ksPropertySets[new Guid ("{1D58C920-AC9B-11CF-A5D6-28DB04C10000}")] = new KsPropertySet ("KSPROPSETID_Connection", typeof (KSPROPERTY_CONNECTION));
            ksPropertySets[new Guid ("{0A3D1C5D-5243-4819-9ED0-AEE8044CEE2B}")] = new KsPropertySet ("KSPROPSETID_MemoryTransport", typeof (KSPROPERTY_MEMORY_TRANSPORT));
            ksPropertySets[new Guid ("{CF6E4342-EC87-11CF-A130-0020AFD156E4}")] = "KSPROPSETID_StreamAllocator";
            ksPropertySets[new Guid ("{1FDD8EE1-9CD3-11D0-82AA-0000F822FE8A}")] = "KSPROPSETID_StreamInterface";
            ksPropertySets[new Guid ("{65AABA60-98AE-11CF-A10D-0020AFD156E4}")] = new KsPropertySet ("KSPROPSETID_Stream", typeof (KSPROPERTY_STREAM));
            ksPropertySets[new Guid ("{DF12A4C0-AC17-11CF-A5D6-28DB04C10000}")] = new KsPropertySet ("KSPROPSETID_Clock", typeof (KSPROPERTY_CLOCK));

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
            ksPropertySets[new Guid ("{E73FACE3-2880-4902-B799-88D0CD634E0F}")] = new KsPropertySet ("KSPROPSETID_VramCapture", typeof (KSPROPERTY_VIDMEM_TRANSPORT));
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

            // Property Flags
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

            // Major formats
            ksMajorFormats[new Guid ("{00000000-0000-0000-0000-000000000000}")] = "KSDATAFORMAT_TYPE_WILDCARD";
            ksMajorFormats[new Guid ("{E436EB83-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_TYPE_STREAM";

            ksMajorFormats[new Guid ("{73646976-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_TYPE_VIDEO";
            ksMajorFormats[new Guid ("{73647561-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_TYPE_AUDIO";
            ksMajorFormats[new Guid ("{73747874-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_TYPE_TEXT";
            ksMajorFormats[new Guid ("{E725D360-62CC-11CF-A5D6-28DB04C10000}")] = "KSDATAFORMAT_TYPE_MUSIC";
            ksMajorFormats[new Guid ("{7364696D-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_TYPE_MIDI";
            ksMajorFormats[new Guid ("{36523B11-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_TYPE_STANDARD_ELEMENTARY_STREAM";
            ksMajorFormats[new Guid ("{36523B12-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_TYPE_STANDARD_PES_PACKET";
            ksMajorFormats[new Guid ("{36523B13-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_TYPE_STANDARD_PACK_HEADER";
            ksMajorFormats[new Guid ("{E06D8020-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_TYPE_MPEG2_PES";
            ksMajorFormats[new Guid ("{E06D8022-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_TYPE_MPEG2_PROGRAM";
            ksMajorFormats[new Guid ("{E06D8023-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_TYPE_MPEG2_TRANSPORT";
            ksMajorFormats[new Guid ("{0482DDE1-7817-11CF-8A03-00AA006ECB65}")] = "KSDATAFORMAT_TYPE_ANALOGVIDEO";
            ksMajorFormats[new Guid ("{0482DEE1-7817-11CF-8A03-00AA006ECB65}")] = "KSDATAFORMAT_TYPE_ANALOGAUDIO";
            ksMajorFormats[new Guid ("{F72A76E1-EB0A-11D0-ACE4-0000C0CC16BA}")] = "KSDATAFORMAT_TYPE_VBI";
            ksMajorFormats[new Guid ("{E757BCA0-39AC-11D1-A9F5-00C04FBBDE8F}")] = "KSDATAFORMAT_TYPE_NABTS";
            ksMajorFormats[new Guid ("{670AEA80-3A82-11D0-B79B-00AA003767A7}")] = "KSDATAFORMAT_TYPE_AUXLine21Data";
            ksMajorFormats[new Guid ("{ED0B916A-044D-11D1-AA78-00C04FC31D60}")] = "KSDATAFORMAT_TYPE_DVD_ENCRYPTED_PACK";

            // Sub formats
            ksSubFormats[new Guid ("{00000000-0000-0000-0000-000000000000}")] = "KSDATAFORMAT_SUBTYPE_WILDCARD";
            ksSubFormats[new Guid ("{E436EB8E-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_NONE";

            ksSubFormats[new Guid ("{00000000-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_WAVEFORMATEX";
            ksSubFormats[new Guid ("{6DBA3190-67BD-11CF-A0F7-0020AFD156E4}")] = "KSDATAFORMAT_SUBTYPE_ANALOG";
            ksSubFormats[new Guid ("{00000001-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_PCM";
            ksSubFormats[new Guid ("{00000003-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_IEEE_FLOAT";
            ksSubFormats[new Guid ("{00000009-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_DRM";
            ksSubFormats[new Guid ("{00000006-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_ALAW";
            ksSubFormats[new Guid ("{00000007-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_MULAW";
            ksSubFormats[new Guid ("{00000002-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_ADPCM";
            ksSubFormats[new Guid ("{00000050-0000-0010-8000-00AA00389B71}")] = "KSDATAFORMAT_SUBTYPE_MPEG";
            ksSubFormats[new Guid ("{4995DAEE-9EE6-11D0-A40E-00A0C9223196}")] = "KSDATAFORMAT_SUBTYPE_RIFF";
            ksSubFormats[new Guid ("{E436EB8B-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_RIFFWAVE";
            ksSubFormats[new Guid ("{1D262760-E957-11CF-A5D6-28DB04C10000}")] = "KSDATAFORMAT_SUBTYPE_MIDI";
            ksSubFormats[new Guid ("{2CA15FA0-6CFE-11CF-A5D6-28DB04C10000}")] = "KSDATAFORMAT_SUBTYPE_MIDI_BUS";
            ksSubFormats[new Guid ("{4995DAF0-9EE6-11D0-A40E-00A0C9223196}")] = "KSDATAFORMAT_SUBTYPE_RIFFMIDI";
            ksSubFormats[new Guid ("{36523B21-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_VIDEO";
            ksSubFormats[new Guid ("{36523B22-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_AUDIO";
            ksSubFormats[new Guid ("{36523B23-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_VIDEO";
            ksSubFormats[new Guid ("{36523B24-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_AUDIO";
            ksSubFormats[new Guid ("{36523B25-8EE5-11D1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SUBTYPE_STANDARD_AC3_AUDIO";
            ksSubFormats[new Guid ("{A0AF4F81-E163-11D0-BAD9-00609744111A}")] = "KSDATAFORMAT_SUBTYPE_DSS_VIDEO";
            ksSubFormats[new Guid ("{A0AF4F82-E163-11D0-BAD9-00609744111A}")] = "KSDATAFORMAT_SUBTYPE_DSS_AUDIO";
            ksSubFormats[new Guid ("{E436EB80-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_MPEG1Packet";
            ksSubFormats[new Guid ("{E436EB81-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_MPEG1Payload";
            ksSubFormats[new Guid ("{E436EB86-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_MPEG1Video";
            ksSubFormats[new Guid ("{E06D8026-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_MPEG2_VIDEO";
            ksSubFormats[new Guid ("{E06D802B-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_MPEG2_AUDIO";
            ksSubFormats[new Guid ("{E06D8032-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_LPCM_AUDIO";
            ksSubFormats[new Guid ("{E06D802C-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_AC3_AUDIO";
            ksSubFormats[new Guid ("{E06D8033-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_DTS_AUDIO";
            ksSubFormats[new Guid ("{E06D8034-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_SDDS_AUDIO";
            ksSubFormats[new Guid ("{E06D802D-DB46-11CF-B4D1-00805F6CBBEA}")] = "KSDATAFORMAT_SUBTYPE_SUBPICTURE";
            ksSubFormats[new Guid ("{5A9B6A40-1A22-11D1-BAD9-00609744111A}")] = "KSDATAFORMAT_SUBTYPE_VPVideo";
            ksSubFormats[new Guid ("{5A9B6A41-1A22-11D1-BAD9-00609744111A}")] = "KSDATAFORMAT_SUBTYPE_VPVBI";
            ksSubFormats[new Guid ("{CA20D9A0-3E3E-11D1-9BF9-00C04FBBDEBF}")] = "KSDATAFORMAT_SUBTYPE_RAW8";
            ksSubFormats[new Guid ("{33214CC1-011F-11D2-B4B1-00A0D102CFBE}")] = "KSDATAFORMAT_SUBTYPE_CC";
            ksSubFormats[new Guid ("{F72A76E2-EB0A-11D0-ACE4-0000C0CC16BA}")] = "KSDATAFORMAT_SUBTYPE_NABTS";
            ksSubFormats[new Guid ("{F72A76E3-EB0A-11D0-ACE4-0000C0CC16BA}")] = "KSDATAFORMAT_SUBTYPE_TELETEXT";
            ksSubFormats[new Guid ("{E757BCA1-39AC-11D1-A9F5-00C04FBBDE8F}")] = "KSDATAFORMAT_SUBTYPE_NABTS_FEC";
            ksSubFormats[new Guid ("{E436EB7F-524F-11CE-9F53-0020AF0BA770}")] = "KSDATAFORMAT_SUBTYPE_OVERLAY";
            ksSubFormats[new Guid ("{6E8D4A22-310C-11D0-B79A-00AA003767A7}")] = "KSDATAFORMAT_SUBTYPE_Line21_BytePair";
            ksSubFormats[new Guid ("{6E8D4A23-310C-11D0-B79A-00AA003767A7}")] = "KSDATAFORMAT_SUBTYPE_Line21_GOPPacket";

            ksSubFormats[new Guid ("{4C504C43-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_CLPL";
            ksSubFormats[new Guid ("{56595559-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_YUYV";
            ksSubFormats[new Guid ("{56555949-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IYUV";
            ksSubFormats[new Guid ("{39555659-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_YVU9";
            ksSubFormats[new Guid ("{31313459-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Y411";
            ksSubFormats[new Guid ("{50313459-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Y41P";
            ksSubFormats[new Guid ("{32595559-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_YUY2";
            ksSubFormats[new Guid ("{55595659-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_YVYU";
            ksSubFormats[new Guid ("{59565955-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_UYVY";
            ksSubFormats[new Guid ("{31313259-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Y211";
            ksSubFormats[new Guid ("{524A4C43-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_CLJR";
            ksSubFormats[new Guid ("{39304649-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IF09";
            ksSubFormats[new Guid ("{414C5043-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_CPLA";
            ksSubFormats[new Guid ("{47504A4D-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_MJPG";
            ksSubFormats[new Guid ("{4A4D5654-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_TVMJ";
            ksSubFormats[new Guid ("{454B4157-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_WAKE";
            ksSubFormats[new Guid ("{43434643-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_CFCC";
            ksSubFormats[new Guid ("{47504A49-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IJPG";
            ksSubFormats[new Guid ("{6D756C50-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Plum";
            ksSubFormats[new Guid ("{53435644-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_DVCS";
            ksSubFormats[new Guid ("{34363248-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_H264";
            ksSubFormats[new Guid ("{44535644-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_DVSD";
            ksSubFormats[new Guid ("{4656444D-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_MDVF";
            ksSubFormats[new Guid ("{E436EB78-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB1";
            ksSubFormats[new Guid ("{E436EB79-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB4";
            ksSubFormats[new Guid ("{E436EB7A-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB8";
            ksSubFormats[new Guid ("{E436EB7B-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB565";
            ksSubFormats[new Guid ("{E436EB7C-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB555";
            ksSubFormats[new Guid ("{E436EB7D-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB24";
            ksSubFormats[new Guid ("{E436EB7E-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_RGB32";
            ksSubFormats[new Guid ("{297C55AF-E209-4CB3-B757-C76D6B9C88A8}")] = "MEDIASUBTYPE_ARGB1555";
            ksSubFormats[new Guid ("{6E6415E6-5C24-425F-93CD-80102B3D1CCA}")] = "MEDIASUBTYPE_ARGB4444";
            ksSubFormats[new Guid ("{773C9AC0-3274-11D0-B724-00AA006C1A01}")] = "MEDIASUBTYPE_ARGB32";
            ksSubFormats[new Guid ("{2F8BB76D-B644-4550-ACF3-D30CAA65D5C5}")] = "MEDIASUBTYPE_A2R10G10B10";
            ksSubFormats[new Guid ("{576F7893-BDF6-48C4-875F-AE7B81834567}")] = "MEDIASUBTYPE_A2B10G10R10";
            ksSubFormats[new Guid ("{56555941-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_AYUV";
            ksSubFormats[new Guid ("{34344941-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_AI44";
            ksSubFormats[new Guid ("{34344149-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IA44";
            ksSubFormats[new Guid ("{32335237-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_RGB32_D3D_DX7_RT";
            ksSubFormats[new Guid ("{36315237-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_RGB16_D3D_DX7_RT";
            ksSubFormats[new Guid ("{38384137-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB32_D3D_DX7_RT";
            ksSubFormats[new Guid ("{34344137-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB4444_D3D_DX7_RT";
            ksSubFormats[new Guid ("{35314137-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB1555_D3D_DX7_RT";
            ksSubFormats[new Guid ("{32335239-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_RGB32_D3D_DX9_RT";
            ksSubFormats[new Guid ("{36315239-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_RGB16_D3D_DX9_RT";
            ksSubFormats[new Guid ("{38384139-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB32_D3D_DX9_RT";
            ksSubFormats[new Guid ("{34344139-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB4444_D3D_DX9_RT";
            ksSubFormats[new Guid ("{35314139-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_ARGB1555_D3D_DX9_RT";
            ksSubFormats[new Guid ("{32315659-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_YV12";
            ksSubFormats[new Guid ("{3231564E-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_NV12";
            ksSubFormats[new Guid ("{31434D49-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IMC1";
            ksSubFormats[new Guid ("{32434D49-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IMC2";
            ksSubFormats[new Guid ("{33434D49-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IMC3";
            ksSubFormats[new Guid ("{34434D49-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_IMC4";
            ksSubFormats[new Guid ("{30343353-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_S340";
            ksSubFormats[new Guid ("{32343353-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_S342";
            ksSubFormats[new Guid ("{E436EB82-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_MPEG1SystemStream";
            ksSubFormats[new Guid ("{E436EB84-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_MPEG1System";
            ksSubFormats[new Guid ("{E436EB85-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_MPEG1VideoCD";
            ksSubFormats[new Guid ("{E436EB87-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_MPEG1Audio";
            ksSubFormats[new Guid ("{E436EB88-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_Avi";
            ksSubFormats[new Guid ("{3DB80F90-9412-11D1-ADED-0000F8754B99}")] = "MEDIASUBTYPE_Asf";
            ksSubFormats[new Guid ("{E436EB89-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_QTMovie";
            ksSubFormats[new Guid ("{617A7072-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Rpza";
            ksSubFormats[new Guid ("{20636D73-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Smc";
            ksSubFormats[new Guid ("{20656C72-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Rle";
            ksSubFormats[new Guid ("{6765706A-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_Jpeg";
            ksSubFormats[new Guid ("{E436EB8A-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_PCMAudio_Obsolete";
            ksSubFormats[new Guid ("{E436EB8C-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_AU";
            ksSubFormats[new Guid ("{E436EB8D-524F-11CE-9F53-0020AF0BA770}")] = "MEDIASUBTYPE_AIFF";
            ksSubFormats[new Guid ("{64737664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dvsd";
            ksSubFormats[new Guid ("{64687664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dvhd";
            ksSubFormats[new Guid ("{6C737664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dvsl";
            ksSubFormats[new Guid ("{35327664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dv25";
            ksSubFormats[new Guid ("{30357664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dv50";
            ksSubFormats[new Guid ("{31687664-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_dvh1";
            ksSubFormats[new Guid ("{6E8D4A24-310C-11D0-B79A-00AA003767A7}")] = "MEDIASUBTYPE_Line21_VBIRawData";
            ksSubFormats[new Guid ("{0AF414BC-4ED2-445E-9839-8F095568AB3C}")] = "MEDIASUBTYPE_708_608Data";
            ksSubFormats[new Guid ("{F52ADDAA-36F0-43F5-95EA-6D866484262A}")] = "MEDIASUBTYPE_DtvCcData";
            ksSubFormats[new Guid ("{2791D576-8E7A-466F-9E90-5D3F3083738B}")] = "MEDIASUBTYPE_WSS";
            ksSubFormats[new Guid ("{A1B3F620-9792-4D8D-81A4-86AF25772090}")] = "MEDIASUBTYPE_VPS";

            ksSubFormats[new Guid ("{30323449-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_I420";
            ksSubFormats[new Guid ("{32323450-0000-0010-8000-00AA00389B71}")] = "MEDIASUBTYPE_P422";
            ksSubFormats[new Guid ("{1d4a45f2-e5f6-4b44-8388-f0ae5c0e0c37}")] = "MEDIASUBTYPE_VIDEOIMAGE";
            ksSubFormats[new Guid ("{3334504D-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_MP43";
            ksSubFormats[new Guid ("{5334504D-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_MP4S";
            ksSubFormats[new Guid ("{3253344D-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_M4S2";
            ksSubFormats[new Guid ("{31564D57-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMV1";
            ksSubFormats[new Guid ("{32564D57-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMV2";
            ksSubFormats[new Guid ("{3153534D-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_MSS1";
            ksSubFormats[new Guid ("{00000162-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMAudioV9";
            ksSubFormats[new Guid ("{00000163-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMAudio_Lossless";
            ksSubFormats[new Guid ("{3253534D-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_MSS2";
            ksSubFormats[new Guid ("{0000000A-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMSP1";
            ksSubFormats[new Guid ("{0000000B-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMSP2";
            ksSubFormats[new Guid ("{33564D57-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMV3";
            ksSubFormats[new Guid ("{50564D57-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMVP";
            ksSubFormats[new Guid ("{32505657-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WVP2";
            ksSubFormats[new Guid ("{41564D57-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMVA";
            ksSubFormats[new Guid ("{31435657-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WVC1";
            ksSubFormats[new Guid ("{00000161-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_WMAudioV8";
            ksSubFormats[new Guid ("{00000130-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_ACELPnet";
            ksSubFormats[new Guid ("{00000055-0000-0010-8000-00AA00389B71}")] = "WMMEDIASUBTYPE_MP3";
            ksSubFormats[new Guid ("{776257D4-C627-41CB-8F81-7AC7FF1C40CC}")] = "WMMEDIASUBTYPE_WebStream";

            // Specifiers
            ksSpecifiers[new Guid ("{00000000-0000-0000-0000-000000000000}")] = "KSDATAFORMAT_SPECIFIER_WILDCARD";
            ksSpecifiers[new Guid ("{AA797B40-E974-11CF-A5D6-28DB04C10000}")] = "KSDATAFORMAT_SPECIFIER_FILENAME";
            ksSpecifiers[new Guid ("{65E8773C-8F56-11D0-A3B9-00A0C9223196}")] = "KSDATAFORMAT_SPECIFIER_FILEHANDLE";
            ksSpecifiers[new Guid ("{0F6417D6-C318-11D0-A43F-00A0C9223196}")] = "KSDATAFORMAT_SPECIFIER_NONE";

            ksSpecifiers[new Guid ("{AD98D184-AAC3-11D0-A41C-00A0C9223196}")] = "KSDATAFORMAT_SPECIFIER_VC_ID";
            ksSpecifiers[new Guid ("{05589f81-c356-11ce-bf01-00aa0055595a}")] = "KSDATAFORMAT_SPECIFIER_WAVEFORMATEX";
            ksSpecifiers[new Guid ("{518590a2-a184-11d0-8522-00c04fd9baf3}")] = "KSDATAFORMAT_SPECIFIER_DSOUND";
            ksSpecifiers[new Guid ("{36523B31-8EE5-11d1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_VIDEO";
            ksSpecifiers[new Guid ("{36523B32-8EE5-11d1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_AUDIO";
            ksSpecifiers[new Guid ("{36523B33-8EE5-11d1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_VIDEO";
            ksSpecifiers[new Guid ("{36523B34-8EE5-11d1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_AUDIO";
            ksSpecifiers[new Guid ("{36523B35-8EE5-11d1-8CA3-0060B057664A}")] = "KSDATAFORMAT_SPECIFIER_DIALECT_AC3_AUDIO";
            ksSpecifiers[new Guid ("{05589f82-c356-11ce-bf01-00aa0055595a}")] = "KSDATAFORMAT_SPECIFIER_MPEG1_VIDEO";
            ksSpecifiers[new Guid ("{e06d80e3-db46-11cf-b4d1-00805f6cbbea}")] = "KSDATAFORMAT_SPECIFIER_MPEG2_VIDEO";
            ksSpecifiers[new Guid ("{e06d80e5-db46-11cf-b4d1-00805f6cbbea}")] = "KSDATAFORMAT_SPECIFIER_MPEG2_AUDIO";
            ksSpecifiers[new Guid ("{e06d80e6-db46-11cf-b4d1-00805f6cbbea}")] = "KSDATAFORMAT_SPECIFIER_LPCM_AUDIO";
            ksSpecifiers[new Guid ("{e06d80e4-db46-11cf-b4d1-00805f6cbbea}")] = "KSDATAFORMAT_SPECIFIER_AC3_AUDIO";
            ksSpecifiers[new Guid ("{05589f80-c356-11ce-bf01-00aa0055595a}")] = "KSDATAFORMAT_SPECIFIER_VIDEOINFO";
            ksSpecifiers[new Guid ("{f72a76A0-eb0a-11d0-ace4-0000c0cc16ba}")] = "KSDATAFORMAT_SPECIFIER_VIDEOINFO2";
            ksSpecifiers[new Guid ("{0482dde0-7817-11cf-8a03-00aa006ecb65}")] = "KSDATAFORMAT_SPECIFIER_ANALOGVIDEO";
            ksSpecifiers[new Guid ("{f72a76e0-eb0a-11d0-ace4-0000c0cc16ba}")] = "KSDATAFORMAT_SPECIFIER_VBI";

            // Memory types
            ksMemoryTypes[new Guid ("{00000000-0000-0000-0000-000000000000}")] = "KSMEMORY_TYPE_WILDCARD";
            ksMemoryTypes[new Guid ("{091BB638-603F-11D1-B067-00A0C9062802}")] = "KSMEMORY_TYPE_SYSTEM";
            ksMemoryTypes[new Guid ("{8CB0FC28-7893-11D1-B069-00A0C9062802}")] = "KSMEMORY_TYPE_USER";
            ksMemoryTypes[new Guid ("{D833F8F8-7894-11D1-B069-00A0C9062802}")] = "KSMEMORY_TYPE_KERNEL_PAGED";
            ksMemoryTypes[new Guid ("{4A6D5FC4-7895-11D1-B069-00A0C9062802}")] = "KSMEMORY_TYPE_KERNEL_NONPAGED";
            ksMemoryTypes[new Guid ("{091BB639-603F-11D1-B067-00A0C9062802}")] = "KSMEMORY_TYPE_DEVICE_UNKNOWN";

            // Bus types TBD

            // Allocator flags: Options (create)
            ksAllocatorCreateFlags.Add (new KeyValuePair<uint, string> (0x00000001, "OPTIONF_COMPATIBLE"));
            ksAllocatorCreateFlags.Add (new KeyValuePair<uint, string> (0x00000002, "OPTIONF_SYSTEM_MEMORY"));

            // Allocator flags: Requirements (query)
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000001, "REQUIREMENTF_INPLACE_MODIFIER"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000002, "REQUIREMENTF_SYSTEM_MEMORY"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000004, "REQUIREMENTF_FRAME_INTEGRITY"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000008, "REQUIREMENTF_MUST_ALLOCATE"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x80000000, "REQUIREMENTF_PREFERENCES_ONLY"));

            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000010, "FLAG_PARTIAL_READ_SUPPORT"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000020, "FLAG_DEVICE_SPECIFIC"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000040, "FLAG_CAN_ALLOCATE"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000080, "FLAG_INSIST_ON_FRAMESIZE_RATIO"));

            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000100, "FLAG_NO_FRAME_INTEGRITY"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000200, "FLAG_MULTIPLE_OUTPUT"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000400, "FLAG_CYCLE"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00000800, "FLAG_ALLOCATOR_EXISTS"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00001000, "FLAG_INDEPENDENT_RANGES"));
            ksAllocatorQueryFlags.Add (new KeyValuePair<uint, string> (0x00002000, "FLAG_ATTENTION_STEPPING"));

            // Event request types
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000001, "KSEVENT_TYPE_ENABLE"));
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000002, "KSEVENT_TYPE_ONESHOT"));
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000004, "KSEVENT_TYPE_ENABLEBUFFERED"));
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000100, "KSEVENT_TYPE_SETSUPPORT"));
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000200, "KSEVENT_TYPE_BASICSUPPORT"));
            ksEventFlags.Add (new KeyValuePair<uint,string> (0x00000400, "KSEVENT_TYPE_QUERYBUFFER"));

            ksEventFlags.Add (new KeyValuePair<uint, string> (0x10000000, "KSEVENT_TYPE_TOPOLOGY"));

            // File alignments
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x00000000, "FILE_BYTE_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x00000001, "FILE_WORD_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x00000003, "FILE_LONG_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x00000007, "FILE_QUAD_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x0000000f, "FILE_OCTA_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x0000001f, "FILE_32_BYTE_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x0000003f, "FILE_64_BYTE_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x0000007f, "FILE_128_BYTE_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x000000ff, "FILE_256_BYTE_ALIGNMENT"));
            fileAlignmentFlags.Add (new KeyValuePair<uint, string> (0x000001ff, "FILE_512_BYTE_ALIGNMENT"));

            VisualizeDump (dump);
        }

        private void VisualizeDump (Dump dump)
        {
            Dictionary<uint, VisualSession> sessions = new Dictionary<uint,VisualSession> ();

            foreach (KeyValuePair<uint, Event> pair in dump.Events)
            {
                Event ev = pair.Value;

                // HACK #1
                //if (ev.ThreadId != 7028 && ev.ThreadId != 3152)
                //    continue;
                //else if (ev.ThreadId == 7028 && ev.Id < 138)
                //    continue;
                if (ev.Id > 2200)
                    continue;

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
                    string[] tokens = node.InnerText.Trim ().Split (new string[] { "::" }, 2, StringSplitOptions.None);
                    string funcNameShort = tokens[tokens.Length - 1];

                    if (funcNameShort == "DeviceIoControl")
                    {
                        tr = CreateTransactionFromDeviceIoControl (ev, eventRoot);
                    }
                    else if (funcNameShort == "GetOverlappedResult")
                    {
                        tr = CreateTransactionFromGetOverlappedResult (ev, eventRoot);
                    }
                    else if (funcNameShort == "KsCreatePin")
                    {
                        tr = CreateTransactionFromKsCreatePin (ev, eventRoot);
                    }
                    else if (funcNameShort == "KsOpenDefaultDevice")
                    {
                        tr = CreateTransactionFromKsOpenDefaultDevice (ev, eventRoot);
                    }
                    else if (funcNameShort == "CloseHandle")
                    {
                        tr = CreateTransactionFromCloseHandle (ev, eventRoot);
                    }
                    else
                    {
                        tr = new VisualTransaction (ev.Id, TransactionDirection.In, ev.Timestamp);
                        tr.HeadlineText = funcNameShort;
                        tr.AddHeaderField ("Id", ev.Id);
                    }
                }
                else
                {
                    tr = new VisualTransaction (ev.Id, ev.Timestamp);
                    tr.HeadlineText = String.Format ("<{0}>", ev.Type);
                    tr.AddHeaderField ("Id", ev.Id);

                    if (ev.Type == DumpEventType.AsyncResult)
                    {
                        uint requestEventId = Convert.ToUInt32 (eventRoot.SelectSingleNode ("/event/requestId").InnerText);
                        tr.AddHeaderField ("RequestId", requestEventId);

                        if (pendingReadStreamRequests.ContainsKey (requestEventId))
                        {
                            pendingReadStreamRequests.Remove (requestEventId);

                            XmlNode dataNode = eventRoot.SelectSingleNode ("/event/data/value");
                            tr.BodyText = KsReadStreamDataToString (dataNode, "out");
                        }
                    }
                }

                if (tr != null)
                    session.Transactions.Add (tr);
            }

            VisualSession[] sessionsArray = new VisualSession[sessions.Count];
            sessions.Values.CopyTo (sessionsArray, 0);
            view.Sessions = sessionsArray;

            pendingReadStreamRequests.Clear ();
        }

        private string KsReadStreamDataToString (XmlNode dataNode, string directionContext)
        {
            StringBuilder body = new StringBuilder ();

            XmlNode streamHdrNode = dataNode.SelectSingleNode ("value[@type='KSSTREAM_HEADER']");
            byte[] streamHdrBuf = Convert.FromBase64String (streamHdrNode.InnerText);
            ByteArrayReader streamHdrReader = new ByteArrayReader (streamHdrBuf);

            uint size = streamHdrReader.ReadU32LE ();
            uint typeSpecificFlags = streamHdrReader.ReadU32LE ();

            long presentationTime = streamHdrReader.ReadI64LE ();
            uint presentationNumerator = streamHdrReader.ReadU32LE ();
            uint presentationDenominator = streamHdrReader.ReadU32LE ();

            long duration = streamHdrReader.ReadI64LE ();
            uint frameExtent = streamHdrReader.ReadU32LE ();
            uint dataUsed = streamHdrReader.ReadU32LE ();
            uint data = streamHdrReader.ReadU32LE ();
            uint optionsFlags = streamHdrReader.ReadU32LE ();

            body.AppendFormat ("[lpOutBuffer direction='{0}']\r\nKSSTREAM_HEADER:", directionContext);
            body.AppendFormat ("\r\n               Size: {0}", size);
            body.AppendFormat ("\r\n  TypeSpecificFlags: 0x{0:x8}", typeSpecificFlags);
            body.AppendFormat ("\r\n   PresentationTime: (Time={0}, {1}/{2})", presentationTime, presentationNumerator, presentationDenominator);
            body.AppendFormat ("\r\n           Duration: {0}", duration);
            body.AppendFormat ("\r\n        FrameExtent: {0}", frameExtent);
            body.AppendFormat ("\r\n           DataUsed: {0}", dataUsed);
            body.AppendFormat ("\r\n               Data: 0x{0:x8}", data);
            body.AppendFormat ("\r\n       OptionsFlags: 0x{0:x8}", optionsFlags);

            string streamHdrRemainder = streamHdrReader.ReadRemainingBytesAsHexDump ();
            if (streamHdrRemainder != null)
                body.AppendFormat ("\r\n\r\n[lpOutBuffer direction='{0}']\r\nRemainder:\r\n{1}", directionContext, streamHdrRemainder);

            XmlNode frameInfoNode = dataNode.SelectSingleNode ("value[@type='KS_FRAME_INFO']");
            byte[] frameInfoBuf = Convert.FromBase64String (frameInfoNode.InnerText);
            ByteArrayReader frameInfoReader = new ByteArrayReader (frameInfoBuf);

            uint extendedHeaderSize = frameInfoReader.ReadU32LE ();
            uint frameFlags = frameInfoReader.ReadU32LE ();
            long pictureNumber = frameInfoReader.ReadI64LE ();
            long dropCount = frameInfoReader.ReadI64LE ();

            uint directDraw = frameInfoReader.ReadU32LE ();
            uint surfaceHandle = frameInfoReader.ReadU32LE ();
            Rectangle directDrawRect = frameInfoReader.ReadRectLE ();

            uint reserved1 = frameInfoReader.ReadU32LE ();
            uint reserved2 = frameInfoReader.ReadU32LE ();
            uint reserved3 = frameInfoReader.ReadU32LE ();
            uint reserved4 = frameInfoReader.ReadU32LE ();

            body.AppendFormat ("\r\nKS_FRAME_INFO:");
            body.AppendFormat ("\r\n  ExtendedHeaderSize: {0}", extendedHeaderSize);
            body.AppendFormat ("\r\n        dwFrameFlags: 0x{0:x8}", frameFlags);
            body.AppendFormat ("\r\n       PictureNumber: {0}", pictureNumber);
            body.AppendFormat ("\r\n           DropCount: {0}", dropCount);
            body.AppendFormat ("\r\n         hDirectDraw: 0x{0:x8}", directDraw);
            body.AppendFormat ("\r\n      hSurfaceHandle: 0x{0:x8}", surfaceHandle);
            body.AppendFormat ("\r\n      DirectDrawRect: ({0}, {1}, {2}, {3})", directDrawRect.Left, directDrawRect.Top, directDrawRect.Right, directDrawRect.Bottom);
            body.AppendFormat ("\r\n           Reserved1: {0}", reserved1);
            body.AppendFormat ("\r\n           Reserved2: {0}", reserved2);
            body.AppendFormat ("\r\n           Reserved3: {0}", reserved3);
            body.AppendFormat ("\r\n           Reserved4: {0}", reserved4);

            string frameInfoRemainder = frameInfoReader.ReadRemainingBytesAsHexDump ();
            if (frameInfoRemainder != null)
                body.AppendFormat ("\r\n\r\n[lpOutBuffer direction='{0}']\r\nRemainder:\r\n{1}", directionContext, frameInfoRemainder);

            return body.ToString ();
        }

        private string FunctionCallArgListToString (XmlElement eventRoot)
        {
            List<string> argList = new List<string> ();
            foreach (XmlNode node in eventRoot.SelectNodes ("/event/arguments[@direction='in']/argument/value"))
            {
                argList.Add (node.Attributes["value"].Value);
            }

            return String.Join (", ", argList.ToArray ());
        }

        private VisualTransaction CreateTransactionFromDeviceIoControl (Event ev, XmlElement eventRoot)
        {
            XmlNode handleNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[1]/value");
            XmlNode codeNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[2]/value");
            XmlNode retValNode = eventRoot.SelectSingleNode ("/event/returnValue/value");
            XmlNode lastErrNode = eventRoot.SelectSingleNode ("/event/lastError");

            string handleStr = null, codeStr = null, retValStr = null, lastErrStr = null;
            string headline = "DeviceIoControl";

            if (handleNode != null && codeNode != null && retValNode != null && lastErrNode != null)
            {
                handleStr = handleNode.Attributes["value"].Value;

                if (!interestingHandles.Contains (handleStr))
                    interestingHandles.Add (handleStr);

                codeStr = codeNode.Attributes["value"].Value;
                retValStr = retValNode.Attributes["value"].Value;
                lastErrStr = ErrorCodeToString (Convert.ToUInt32 (lastErrNode.Attributes["value"].Value));
                headline += String.Format (" ({0}) => {1}", FunctionCallArgListToString (eventRoot), retValStr.ToUpper ());
            }

            // HACK #2:
            if (lastErrStr == "ERROR_MORE_DATA") // || lastErrStr == "ERROR_NOT_FOUND" || lastErrStr == "ERROR_SET_NOT_FOUND")
                return null;

            VisualTransaction tr = new VisualTransaction (ev.Id, TransactionDirection.Out, ev.Timestamp);
            tr.ContextID = handleStr;
            tr.HeadlineText = headline;
            tr.AddHeaderField ("Id", ev.Id);
            if (lastErrStr != null)
                tr.AddHeaderField ("LastError", lastErrStr);

            ByteArrayReader inBuf = null;
            ByteArrayReader outBufEnter = null;
            ByteArrayReader outBufLeave = null;

            XmlNode node = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[3]/value/value");
            if (node != null)
                inBuf = new ByteArrayReader (Convert.FromBase64String (node.InnerText));

            node = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[5]/value/value");
            if (node != null)
                outBufEnter = new ByteArrayReader (Convert.FromBase64String (node.InnerText));

            node = eventRoot.SelectSingleNode ("/event/arguments[@direction='out']/argument[1]/value/value");
            if (node != null)
                outBufLeave = new ByteArrayReader (Convert.FromBase64String (node.InnerText));

            if (codeStr == "IOCTL_KS_PROPERTY" && inBuf != null)
            {
                Guid propSetGuid = inBuf.ReadGuid ();
                uint rawPropId = inBuf.ReadU32LE ();
                uint propFlags = inBuf.ReadU32LE ();

                string propSetStr, propIdStr, propFlagsStr;

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

                propFlagsStr = BitfieldToString (ksPropertyFlags, propFlags);

                // HACK #3
                //if (propFlagsStr == "GET")
                //    return null;

                if (propSetStr == "KSPROPSETID_Topology")
                    return null;
                else if (propSetStr == "KSPROPSETID_MediaSeeking" && propIdStr == "TIMEFORMAT" && propFlagsStr == "GET")
                    return null;
                else if (propSetStr == "KSPROPSETID_Pin" && propFlagsStr == "GET")
                {
                    List<string> boringIds = new List<string> (new string[] { "CTYPES", "CINSTANCES", "COMMUNICATION", "CONSTRAINEDDATARANGES", "DATAFLOW", "DATARANGES", "DATAINTERSECTION", "NAME" });
                    if (boringIds.Contains (propIdStr))
                        return null;
                }

                StringBuilder body = new StringBuilder ();
                body.AppendFormat ("[lpInBuffer]\r\nKSPROPERTY: {0}, {1}, {2}", propSetStr, propIdStr, propFlagsStr);

                string remainder = inBuf.ReadRemainingBytesAsHexDump ();
                if (remainder != null)
                    body.AppendFormat ("\r\n{0}", remainder);

                if (outBufEnter != null)
                {
                    body.Append ("\r\n\r\n[lpOutBuffer on entry]");

                    if (propSetStr == "KSPROPSETID_Connection" && propIdStr == "DATAFORMAT")
                    {
                        body.AppendFormat ("\r\n{0}", KsDataFormatToString (outBufEnter));
                    }

                    remainder = outBufEnter.ReadRemainingBytesAsHexDump ();
                    if (remainder != null)
                        body.AppendFormat ("\r\n{0}", remainder);
                }

                if (outBufLeave != null)
                {
                    body.Append ("\r\n\r\n[lpOutBuffer on exit]");

                    if (propSetStr == "KSPROPSETID_Connection" && propIdStr == "ALLOCATORFRAMING_EX")
                    {
                        body.Append (KsAllocatorFramingExToString (outBufLeave));
                    }

                    remainder = outBufLeave.ReadRemainingBytesAsHexDump ();
                    if (remainder != null)
                        body.AppendFormat ("\r\n{0}", remainder);
                }

                tr.BodyText = body.ToString ();
            }
            else if (codeStr == "IOCTL_KS_READ_STREAM")
            {
                XmlNode dataNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[5]/value");
                string body = KsReadStreamDataToString (dataNode, "in");

                if (retValStr.ToUpper () == "TRUE")
                {
                    dataNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='out']/argument[1]/value");
                    body += KsReadStreamDataToString (dataNode, "out");
                }
                else if (lastErrStr == "ERROR_IO_PENDING")
                {
                    pendingReadStreamRequests[ev.Id] = true;
                }

                tr.BodyText = body;
            }
            else if (codeStr == "IOCTL_KS_ENABLE_EVENT" && inBuf != null)
            {
                StringBuilder body = new StringBuilder ();

                body.AppendFormat ("[lpInBuffer]\r\nKSEVENT: {0} {1} {2}",
                    inBuf.ReadGuid (), inBuf.ReadU32LE (),
                    BitfieldToString (ksEventFlags, inBuf.ReadU32LE ()));

                string remainder = inBuf.ReadRemainingBytesAsHexDump ();
                if (remainder != null)
                    body.AppendFormat ("\r\n{0}", remainder);

                if (outBufEnter != null)
                {
                    body.Append ("\r\n\r\n[lpOutBuffer on entry]");
                    remainder = outBufEnter.ReadRemainingBytesAsHexDump ();
                    if (remainder != null)
                        body.AppendFormat ("\r\n{0}", remainder);
                }

                if (outBufLeave != null)
                {
                    body.Append ("\r\n\r\n[lpOutBuffer on exit]");
                    remainder = outBufLeave.ReadRemainingBytesAsHexDump ();
                    if (remainder != null)
                        body.AppendFormat ("\r\n{0}", remainder);
                }

                tr.BodyText = body.ToString ();
            }
            else
            {
                List<string> blobs = new List<string> ();
                if (inBuf != null)
                    blobs.Add (inBuf.ReadRemainingBytesAsHexDump ());
                if (outBufEnter != null)
                    blobs.Add (outBufEnter.ReadRemainingBytesAsHexDump ());

                if (blobs.Count > 0)
                    tr.BodyText = String.Join ("\r\n\r\n", blobs.ToArray ());
            }

            return tr;
        }

        private string KsDataFormatToString (ByteArrayReader reader)
        {
            StringBuilder result = new StringBuilder ();

            uint formatSize = reader.ReadU32LE ();
            uint flags = reader.ReadU32LE ();
            uint sampleSize = reader.ReadU32LE ();
            uint reserved = reader.ReadU32LE ();
            Guid majorFormatGuid = reader.ReadGuid ();
            string majorFormatStr = MajorFormatToString (majorFormatGuid);
            Guid subFormatGuid = reader.ReadGuid ();
            string subFormatStr = SubFormatToString (subFormatGuid);
            Guid specifierGuid = reader.ReadGuid ();
            string specifierStr = SpecifierToString (specifierGuid);

            result.Append ("\r\nKSDATAFORMAT:");
            result.AppendFormat ("\r\n   FormatSize: {0}", formatSize);
            result.AppendFormat ("\r\n        Flags: {0}", flags);
            result.AppendFormat ("\r\n   SampleSize: {0}", sampleSize);
            result.AppendFormat ("\r\n     Reserved: {0}", reserved);
            result.AppendFormat ("\r\n  MajorFormat: {0}", majorFormatStr);
            result.AppendFormat ("\r\n    SubFormat: {0}", subFormatStr);
            result.AppendFormat ("\r\n    Specifier: {0}", specifierStr);

            if (specifierStr == "KSDATAFORMAT_SPECIFIER_VIDEOINFO")
            {
                Rectangle sourceRect = reader.ReadRectLE ();
                Rectangle targetRect = reader.ReadRectLE ();
                uint bitRate = reader.ReadU32LE ();
                uint bitErrorRate = reader.ReadU32LE ();
                Int64 avgTimePerFrame = reader.ReadI64LE ();
                double fps = 10000000.0 / avgTimePerFrame;

                result.Append ("\r\nKS_VIDEOINFO:");
                result.AppendFormat ("\r\n         rcSource: ({0}, {1}, {2}, {3})", sourceRect.Left, sourceRect.Top, sourceRect.Right, sourceRect.Bottom);
                result.AppendFormat ("\r\n         rcTarget: ({0}, {1}, {2}, {3})", targetRect.Left, targetRect.Top, targetRect.Right, targetRect.Bottom);
                result.AppendFormat ("\r\n        dwBitRate: {0}", bitRate);
                result.AppendFormat ("\r\n   dwBitErrorRate: {0}", bitErrorRate);
                result.AppendFormat ("\r\n  AvgTimePerFrame: {0} ({1:0.##} fps)", avgTimePerFrame, fps);

                uint size = reader.ReadU32LE ();
                int width = reader.ReadI32LE ();
                int height = reader.ReadI32LE ();
                ushort planes = reader.ReadU16LE ();
                ushort bitCount = reader.ReadU16LE ();
                uint compression = reader.ReadU32LE ();
                uint sizeImage = reader.ReadU32LE ();
                int xPelsPerMeter = reader.ReadI32LE ();
                int yPelsPerMeter = reader.ReadI32LE ();
                uint clrUsed = reader.ReadU32LE ();
                uint clrImportant = reader.ReadU32LE ();

                result.Append ("\r\nKS_BITMAPINFOHEADER:");
                result.AppendFormat ("\r\n           biSize: {0}", size);
                result.AppendFormat ("\r\n          biWidth: {0}", width);
                result.AppendFormat ("\r\n         biHeight: {0}", height);
                result.AppendFormat ("\r\n         biPlanes: {0}", planes);
                result.AppendFormat ("\r\n       biBitCount: {0}", bitCount);
                result.AppendFormat ("\r\n    biCompression: 0x{0:x8}", compression);
                result.AppendFormat ("\r\n      biSizeImage: {0}", sizeImage);
                result.AppendFormat ("\r\n  biXPelsPerMeter: {0}", xPelsPerMeter);
                result.AppendFormat ("\r\n  biYPelsPerMeter: {0}", yPelsPerMeter);
                result.AppendFormat ("\r\n        biClrUsed: {0}", clrUsed);
                result.AppendFormat ("\r\n   biClrImportant: {0}", clrImportant);
            }

            return result.ToString ();
        }

        private string KsAllocatorFramingExToString (ByteArrayReader reader)
        {
            StringBuilder result = new StringBuilder ();

            uint countItems = reader.ReadU32LE ();

            result.Append ("\r\nKSALLOCATOR_FRAMING_EX:");
            result.AppendFormat ("\r\n         CountItems: {0}", countItems);
            result.AppendFormat ("\r\n           PinFlags: 0x{0:x8}", reader.ReadU32LE ());
            result.AppendFormat ("\r\n  OutputCompression: ({0}/{1}, ConstantMargin={2})",
                reader.ReadU32LE (), reader.ReadU32LE (), reader.ReadU32LE ());
            result.AppendFormat ("\r\n          PinWeight: {0}", reader.ReadU32LE ());

            for (int i = 0; i < countItems; i++)
            {
                result.AppendFormat ("\r\n\r\nFramingItem[{0}]:", i);
                result.AppendFormat ("\r\n        MemoryType: {0}", MemoryTypeToString (reader.ReadGuid ()));
                result.AppendFormat ("\r\n           BusType: {0}", BusTypeToString (reader.ReadGuid ()));
                result.AppendFormat ("\r\n       MemoryFlags: {0}", BitfieldToString (ksAllocatorQueryFlags, reader.ReadU32LE ()));
                result.AppendFormat ("\r\n          BusFlags: 0x{0:x8}", reader.ReadU32LE ());
                result.AppendFormat ("\r\n             Flags: {0}", BitfieldToString (ksAllocatorQueryFlags, reader.ReadU32LE ()));
                result.AppendFormat ("\r\n            Frames: {0}", reader.ReadU32LE ());
                result.AppendFormat ("\r\n     FileAlignment: {0}", BitfieldToString (fileAlignmentFlags, reader.ReadU32LE ()));
                result.AppendFormat ("\r\n  MemoryTypeWeight: {0}", reader.ReadU32LE ());
                result.AppendFormat ("\r\n     PhysicalRange: ([{0}, {1}], Stepping={2})",
                    reader.ReadU32LE (), reader.ReadU32LE (), reader.ReadU32LE ());
                result.AppendFormat ("\r\n      FramingRange: (([{0}, {1}], Stepping={2}), InPlaceWeight={3}, NotInPlaceWeight={4})",
                    reader.ReadU32LE (), reader.ReadU32LE (), reader.ReadU32LE (), reader.ReadU32LE (), reader.ReadU32LE ());
            }

            return result.ToString ();
        }

        private VisualTransaction CreateTransactionFromGetOverlappedResult (Event ev, XmlElement eventRoot)
        {
            VisualTransaction tr = new VisualTransaction (ev.Id, TransactionDirection.In, ev.Timestamp);

            string retValStr = eventRoot.SelectSingleNode ("/event/returnValue/value").Attributes["value"].Value;
            string lastErrStr = ErrorCodeToString (Convert.ToUInt32 (eventRoot.SelectSingleNode ("/event/lastError").Attributes["value"].Value));

            tr.HeadlineText = String.Format ("GetOverlappedResult ({0}) => {1}", FunctionCallArgListToString (eventRoot), retValStr.ToUpper ());
            tr.AddHeaderField ("Id", ev.Id);
            tr.AddHeaderField ("LastError", lastErrStr);

            XmlNode handleNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[1]/value");
            tr.ContextID = handleNode.Attributes["value"].Value;

            return tr;
        }

        private VisualTransaction CreateTransactionFromKsCreatePin (Event ev, XmlElement eventRoot)
        {
            VisualTransaction tr = new VisualTransaction (ev.Id, TransactionDirection.In, ev.Timestamp);

            string retValStr = eventRoot.SelectSingleNode ("/event/returnValue/value").Attributes["value"].Value;
            string lastErrStr = ErrorCodeToString (Convert.ToUInt32 (eventRoot.SelectSingleNode ("/event/lastError").Attributes["value"].Value));

            XmlNodeList inNodes = eventRoot.SelectNodes ("/event/arguments[@direction='in']/argument/value");
            string filterHandle = inNodes[0].Attributes["value"].Value;
            string desiredAccess = inNodes[2].Attributes["value"].Value;

            string connHandle = "";
            XmlNode outNode = eventRoot.SelectSingleNode ("/event/arguments[@direction='out']/argument/value/value");
            if (outNode != null)
            {
                connHandle = " => " + outNode.Attributes["value"].Value;
            }

            tr.ContextID = filterHandle;
            tr.HeadlineText = String.Format ("KsCreatePin ({0}, &Connect, {1}, &ConnectionHandle{2}) => {3}", filterHandle, desiredAccess, connHandle, retValStr);
            tr.AddHeaderField ("Id", ev.Id);
            tr.AddHeaderField ("LastError", lastErrStr);

            StringBuilder body = new StringBuilder ();

            byte[] connBytes = Convert.FromBase64String (inNodes[1].SelectSingleNode ("value[@type='KSPIN_CONNECT']").InnerText);
            ByteArrayReader connReader = new ByteArrayReader (connBytes);
            body.Append ("[Connect]:\r\nKSPIN_CONNECT:");
            body.AppendFormat ("\r\n    Interface: ({0}, {1}, {2})", connReader.ReadGuid (), connReader.ReadU32LE (), connReader.ReadU32LE ());
            body.AppendFormat ("\r\n       Medium: ({0}, {1}, {2})", connReader.ReadGuid (), connReader.ReadU32LE (), connReader.ReadU32LE ());
            body.AppendFormat ("\r\n        PinId: {0}", connReader.ReadU32LE ());
            body.AppendFormat ("\r\n  PinToHandle: {0}", connReader.ReadU32LE ());
            body.AppendFormat ("\r\n     Priority: ({0}, {1})", connReader.ReadU32LE (), connReader.ReadU32LE ());

            if (connReader.Remaining > 0)
                throw new Exception ("KSPIN_CONNECT parse error");

            byte[] formatRaw = Convert.FromBase64String (inNodes[1].SelectSingleNode ("value[@type='KSDATAFORMAT']").InnerText);
            ByteArrayReader formatReader = new ByteArrayReader (formatRaw);
            body.AppendFormat ("\r\nKSDATAFORMAT:{0}", KsDataFormatToString (formatReader));

            tr.BodyText = body.ToString ();

            return tr;
        }

        private VisualTransaction CreateTransactionFromKsOpenDefaultDevice (Event ev, XmlElement eventRoot)
        {
            VisualTransaction tr = new VisualTransaction (ev.Id, TransactionDirection.In, ev.Timestamp);

            XmlNode node = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[1]/value/value");
            Guid category = new Guid (Convert.FromBase64String (node.InnerText));

            node = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[2]/value");
            string access = node.Attributes["value"].Value;

            node = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[3]/value");
            string deviceHandleStr = node.Attributes["value"].Value;

            node = eventRoot.SelectSingleNode ("/event/arguments[@direction='out']/argument[1]/value/value");
            if (node != null)
            {
                tr.ContextID = node.Attributes["value"].Value;
                deviceHandleStr += String.Format (" => {0}", tr.ContextID);
            }

            string retValStr = eventRoot.SelectSingleNode ("/event/returnValue/value").Attributes["value"].Value;

            tr.HeadlineText = String.Format ("KsOpenDefaultDevice ({0}, {1}, {2}) => {3}", CategoryToString (category), access, deviceHandleStr, retValStr);
            tr.AddHeaderField ("Id", ev.Id);

            return tr;
        }

        private VisualTransaction CreateTransactionFromCloseHandle (Event ev, XmlElement eventRoot)
        {
            string handleStr = eventRoot.SelectSingleNode ("/event/arguments[@direction='in']/argument[1]/value").Attributes["value"].Value;
            if (!interestingHandles.Contains (handleStr))
                return null;

            VisualTransaction tr = new VisualTransaction (ev.Id, TransactionDirection.In, ev.Timestamp);
            tr.AddHeaderField ("Id", ev.Id);

            string retValStr = eventRoot.SelectSingleNode ("/event/returnValue/value").Attributes["value"].Value;

            tr.ContextID = handleStr;
            tr.HeadlineText = String.Format ("CloseHandle ({0}) => {1}", handleStr, retValStr);

            return tr;
        }

        private string ErrorCodeToString (uint errorCode)
        {
            if (errorCodes.ContainsKey (errorCode))
                return errorCodes[errorCode];
            else
                return Convert.ToString (errorCode);
        }

        private string CategoryToString (Guid categoryGuid)
        {
            if (ksCategories.ContainsKey (categoryGuid))
                return ksCategories[categoryGuid];
            else
                return categoryGuid.ToString ("B");
        }

        private string MajorFormatToString (Guid majorFormatGuid)
        {
            if (ksMajorFormats.ContainsKey (majorFormatGuid))
                return ksMajorFormats[majorFormatGuid];
            else
                return majorFormatGuid.ToString ("B");
        }

        private string SubFormatToString (Guid subFormatGuid)
        {
            if (ksSubFormats.ContainsKey (subFormatGuid))
                return ksSubFormats[subFormatGuid];
            else
                return subFormatGuid.ToString ("B");
        }

        private string SpecifierToString (Guid specifierGuid)
        {
            if (ksSpecifiers.ContainsKey (specifierGuid))
                return ksSpecifiers[specifierGuid];
            else
                return specifierGuid.ToString ("B");
        }

        private string MemoryTypeToString (Guid memoryTypeGuid)
        {
            if (ksMemoryTypes.ContainsKey (memoryTypeGuid))
                return ksMemoryTypes[memoryTypeGuid];
            else
                return memoryTypeGuid.ToString ("B");
        }

        private string BusTypeToString (Guid busTypeGuid)
        {
            if (ksBusTypes.ContainsKey (busTypeGuid))
                return ksBusTypes[busTypeGuid];
            else
                return busTypeGuid.ToString ("B");
        }

        // This should be turned into a class instead
        private string BitfieldToString (List<KeyValuePair<uint, string>> def, uint bits)
        {
            StringBuilder result = new StringBuilder ();

            foreach (KeyValuePair<uint, string> pair in def)
            {
                uint mask = pair.Key;
                string name = pair.Value;

                if ((bits & mask) != 0)
                {
                    if (result.Length > 0)
                        result.Append ("|");
                    result.Append (name);

                    bits &= ~mask;
                }
            }

            if (bits != 0 || result.Length == 0)
            {
                if (result.Length > 0)
                    result.Append ("|");
                result.AppendFormat ("0x{0:x8}", bits);
            }

            return result.ToString ();
        }
    }

    class ByteArrayReader
    {
        private byte[] data = null;
        private int offset = 0;

        public int Remaining
        {
            get { return data.Length - offset; }
        }

        public ByteArrayReader (byte[] data)
        {
            this.data = data;
        }

        public byte[] ReadBytes (int n)
        {
            byte[] buf = new byte[n];
            Array.Copy (data, offset, buf, 0, buf.Length);
            offset += n;
            return buf;
        }

        public ushort ReadU16LE ()
        {
            byte[] buf = ReadBytes (2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse (buf);
            return BitConverter.ToUInt16 (buf, 0);
        }

        public Int32 ReadI32LE ()
        {
            byte[] buf = ReadBytes (4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse (buf);
            return BitConverter.ToInt32 (buf, 0);
        }

        public UInt32 ReadU32LE ()
        {
            byte[] buf = ReadBytes (4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse (buf);
            return BitConverter.ToUInt32 (buf, 0);
        }

        public long ReadI64LE ()
        {
            byte[] buf = ReadBytes (8);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse (buf);
            return BitConverter.ToInt64 (buf, 0);
        }

        public Guid ReadGuid ()
        {
            byte[] buf = ReadBytes (16);
            return new Guid (buf);
        }

        public string ReadRemainingBytesAsHexDump ()
        {
            if (Remaining <= 0)
                return null;

            byte[] buf = ReadBytes (Remaining);
            return StaticUtils.ByteArrayToHexDump (buf);
        }

        public Rectangle ReadRectLE ()
        {
            Int32 left = ReadI32LE ();
            Int32 top = ReadI32LE ();
            Int32 right = ReadI32LE ();
            Int32 bottom = ReadI32LE ();

            return new Rectangle (left, top, right - left, bottom - top);
        }
    }
}
