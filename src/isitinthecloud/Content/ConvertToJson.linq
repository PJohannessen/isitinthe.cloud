<Query Kind="Program">
  <NuGetReference>IPNetwork2</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>System.Net</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
	XDocument document = XDocument.Load("PublicIPs_20170328.xml");
	var regions = document.Root.Elements("Region");
	var ranges = regions.SelectMany(r => r.Elements("IpRange").Select(ipr =>
	{
		var subnet = ipr.Attribute("Subnet").Value;
		IPNetwork ipnetwork = IPNetwork.Parse(subnet);
		string firstUsableUint = IpToInt(ipnetwork.FirstUsable.ToString()).ToString();
		string lastUsableUint = IpToInt(ipnetwork.LastUsable.ToString()).ToString();
		return new
		{
			region = r.Attribute("Name").Value,
			subnet = subnet,
			lower = firstUsableUint,
			upper = lastUsableUint
		};
	}));
	string json = JsonConvert.SerializeObject(ranges);
	System.IO.File.WriteAllText(@"AzurePublicIPs.json", json);
}

uint IpToInt(string ip)
{
	IPAddress address = IPAddress.Parse(ip);
	byte[] bytes = address.GetAddressBytes();
	if (BitConverter.IsLittleEndian)
		Array.Reverse(bytes);
	uint intAddress = BitConverter.ToUInt32(bytes, 0);
	return intAddress;
}