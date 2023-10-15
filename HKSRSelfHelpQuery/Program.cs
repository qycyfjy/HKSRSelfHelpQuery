using System.Text.Json;

internal class Program
{
    private static readonly string RelicDbFilePath = "relic.json";
    private static readonly string StellarDbFilePath = "stellar.json";

    public static async Task Main(string[] args)
    {
        Console.Write("遗物(空白输入表示不获取): ");
        var relicRequestUrlTemplate = Console.ReadLine();
        Console.Write("星琼(空白输入表示不获取): ");
        var stellarRequestUrlTemplate = Console.ReadLine();

        await Mode<RelicData, RelicInfo>(relicRequestUrlTemplate, RelicDbFilePath);
        await Mode<StellarData, StellarInfo>(stellarRequestUrlTemplate, StellarDbFilePath);
    }

    private static async Task Mode<TData, TInfo>(string urlTemplate, string dbFileName) 
        where TData : IData<TInfo> 
        where TInfo : IInfo
    {
        if (string.IsNullOrEmpty(urlTemplate))
        {
            return;
        }
        Db<TInfo> db = new();
        if (File.Exists(dbFileName))
        {
            {
                await using var history = File.OpenRead(dbFileName);
                db = await JsonSerializer.DeserializeAsync<Db<TInfo>>(history);
                var (latest, update) = await Fetch<TData, TInfo>(urlTemplate, db.lastest);
                if (latest == null)
                {
                    Console.WriteLine("No Update");
                    return;
                }

                db.lastest = latest;
                db.relics.AddRange(update);
            }
            File.Delete(dbFileName);
        }
        else
        {
            var (latest, data) = await Fetch<TData, TInfo>(urlTemplate);
            db.lastest = latest!;
            db.relics = data;
        }

        await using var f = File.Create(dbFileName);
        await JsonSerializer.SerializeAsync(f, db);
    }


    private static async Task<(string?, List<TInfo>)> 
        Fetch<TData, TInfo>(string relicRequestUrlTemplate, string? latest = null) 
        where TData : IData<TInfo> 
        where TInfo : IInfo
    {
        var client = new HttpClient();
        string? newLatest = null;
        var result = new List<TInfo>();

        var shouldBreak = false;
        for (var i = 1; i < int.MaxValue; i++)
        {
            Console.WriteLine($"正在获取{i}页");
            var relics = await FetchRelic<Response<TData, TInfo>>(client, string.Format(relicRequestUrlTemplate, i));
            if (!ValidRelicResponse(relics)) break;

            foreach (var relic in relics.data.list)
            {
                if (latest is not null && relic.time == latest)
                {
                    shouldBreak = true;
                    break;
                }

                newLatest ??= relic.time;
                result.Add(relic);
            }

            if (shouldBreak) break;
            var idle_ms = Random.Shared.Next(2000) + 3000;
            await Task.Delay(idle_ms);
        }

        return (newLatest, result);
    }

    private static bool ValidRelicResponse<TData, TInfo>(Response<TData, TInfo>? response) where TData : IData<TInfo>
    {
        return response != null && response.retcode == 0 && response.data.list.Count() != 0;
    }

    private static async Task<ResponseType> FetchRelic<ResponseType>(HttpClient client, string url)
    {
        await using var response = await client.GetStreamAsync(url);
        return await JsonSerializer.DeserializeAsync<ResponseType>(response);
    }
}

public interface IData<Info>
{
    public string page { get; set; }
    public string page_size { get; set; }
    public List<Info> list { get; set; }
}

public interface IInfo
{
    public string time { get; set; }
}

public class Db<Resource>
{
    public string lastest { get; set; }
    public List<Resource> relics { get; set; }
}

public class Response<Data, Info> where Data : IData<Info>
{
    public int retcode { get; set; }
    public string message { get; set; }
    public Data data { get; set; }
}

public class RelicData : IData<RelicInfo>
{
    public string page { get; set; }
    public string page_size { get; set; }
    public List<RelicInfo> list { get; set; }
}

public class RelicInfo : IInfo
{
    public string uid { get; set; }
    public int add_num { get; set; }
    public string action { get; set; }
    public string relic_name { get; set; }
    public int relic_level { get; set; }
    public int relic_rarity { get; set; }
    public string time { get; set; }
}

public class StellarData : IData<StellarInfo>
{
    public string page { get; set; }
    public string page_size { get; set; }
    public List<StellarInfo> list { get; set; }
}

public class StellarInfo : IInfo
{
    public string uid { get; set; }
    public int add_num { get; set; }
    public string action { get; set; }
    public string time { get; set; }
}