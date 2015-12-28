namespace TellySorter.Models
{
    
    using System;
    using Mono.Data.Sqlite;
    
    public class Rule
    {

        public int Id { get; set; }
        public int TvdbShowId { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }

        public Rule(SqliteDataReader data)
        {
            Id = int.Parse(data["id"].ToString());
            TvdbShowId = int.Parse(data["tvdb_show_id"].ToString());
            Type = data["type"].ToString();
            Path = data["path"].ToString();
        }

    }

}
