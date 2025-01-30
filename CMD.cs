using System.Collections.Generic;
using System.Windows.Forms;

namespace loramesh
{
	internal class CMD
	{
		internal string SAVE_CMD_RANGE = "0&1";
		internal string POWER_RANGE = "-9&22";
		internal string CHANNEL_RANGE = "0&99";
		internal string[] BAUD_CMD_PARA = new string[]
		{
			"1200&1200",
			"2400&2400",
			"4800&4800",
			"9600&9600",
			"19200&19200",
			"38400&38400",
			"57600&57600",
			"115200&115200",
			"230400&230400",
			"460800&460800"
		};
		internal string[] PARITY_CMD_PARA = new string[]
		{
			"8N1&0",
			"8E1&1",
			"8O1&2"
		};
		internal string[] RATE_CMD_PARA = new string[]
		{
			"62.5K&0",
			"21.825K&1",
			"7K&2"
		};
		internal string[] BW_CMD_PARA = new string[]
		{
			"125K&00",
			"250K&01",
			"500K&02"
		};
		internal string[] SF_CMD_PARA = new string[]
		{
			"SF5&5",
			"SF6&6",
			"SF7&7",
			"SF8&8",
			"SF9&9",
			"SF10&10",
			"SF11&11",
			"SF12&12"
		};
		internal string[] CR_CMD_PARA = new string[]
		{
			"CR4/5&1",
			"CR4/6&2",
			"CR4/7&3",
			"CR4/8&4"
		};
		internal string[] OPTION_CMD_PARA = new string[]
		{
			"Unicast&1",
			"Multicast&2",
			"Broadcast&3",
			"Anycast&4"
		};
		internal string PANID_RANGE = "0&65535";
		internal string[] TYPE_CMD_PARA = new string[]
		{
			"Router&0",
			"Terminal&1"
		};
		internal string ADDR_RANGE = "0&65535";
		internal string RAD_RANGE = "0&15";
		internal string CAD_RANGE = "2&255";
		internal string RNG_RANGE = "20&65535";
		internal string SCORE_RANGE = "0&15";
		internal string[] ENABLE_PARA = new string[]
		{
			"Disable&0",
			"Enable&1"
		};
		internal string RESET_TIME_RANGE = "0&255";
		internal string TIME_RANGE = "2000&65535";
		internal string KEY_RANGE = "0&2147483647";
		internal string FREQ_RANGE = "410.125&510.125";
		internal string TEST_TIME_RANGE = "0&65535";
		internal string[] PORT_PARA = new string[2]
		{			
			"01&01",
			"14&14"
		};
		internal string MAC_RANGE = "0&4294967295";
		internal string AT_DEVTYPE = "AT+DEVTYPE";
		internal string AT_FWCODE = "AT+FWCODE";
		internal string AT_POWER = "AT+POWER";
		internal string AT_CHANNEL = "AT+CHANNEL";
		internal string AT_UART = "AT+UART";
		internal string AT_RATE = "AT+RATE";
		internal string AT_MODEM = "AT+MODEM";
		internal string AT_OPTION = "AT+OPTION";
		internal string AT_PANID = "AT+PANID";
		internal string AT_TYPE = "AT+TYPE";
		internal string AT_SRC_ADDR = "AT+SRC_ADDR";
		internal string AT_DST_ADDR = "AT+DST_ADDR";
		internal string AT_SRC_PORT = "AT+SRC_PORT";
		internal string AT_DST_PORT = "AT+DST_PORT";
		internal string AT_HEAD = "AT+HEAD";
		internal string AT_SECURITY = "AT+SECURITY";
		internal string AT_RESET_AUX = "AT+RESET_AUX";
		internal string AT_RESET_TIME = "AT+RESET_TIME";
		internal string AT_FILTER_TIME = "AT+FILTER_TIME";
		internal string AT_ACK_TIME = "AT+ACK_TIME";
		internal string AT_ROUTER_TIME = "AT+ROUTER_TIME";
		internal string AT_GROUP_ADD = "AT+GROUP_ADD";
		internal string AT_GROUP_DEL = "AT+GROUP_DEL";
		internal string AT_GROUP_CLR = "AT+GROUP_CLR";
		internal string AT_ROUTER_CLR = "AT+ROUTER_CLR";
		internal string AT_ROUTER_SAVE = "AT+ROUTER_SAVE";
		internal string AT_ROUTER_READ = "AT+ROUTER_READ";
		internal string AT_MAC = "AT+MAC";
		internal string AT_KEY = "AT+KEY";

		public enum P
		{
			R,
			W,
			RW,
		}

		public class CMD_ATT
		{
			public string CMD;
			public int cmd_type;
			public List<CMD_CONTROL> cmd_control = new List<CMD_CONTROL>();
			public P p;
		}

		public class CMD_CONTROL
		{
			public string[] CMD_PARA_LIST;
			public string CMD_RANGE_LIST;
			public Control CONTROL;
			public ComboBox COMBOBOX;
		}
	}
}
