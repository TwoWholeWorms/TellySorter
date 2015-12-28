namespace TellySorter.Models
{
    
    using Mono.Data.Sqlite;
    using System;
    using TVDBSharp.Models;

    public class Series
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string GuessedName { get; set; }
        public int TvdbShowId { get; set; }

        public string ImdbId;
        public string Language;
        public string Zap2ItId;

        public Series(SqliteDataReader data)
        {
            Id = int.Parse(data["id"].ToString());
            Name = data["name"].ToString();
            GuessedName = data["guessed_name"].ToString();
            TvdbShowId = data["tvdb_show_id"].ToString() != "" ? int.Parse(data["tvdb_show_id"].ToString()) : 0;

            ImdbId = data["imdb_id"].ToString();
            Language = data["language"].ToString();
            Zap2ItId = data["zap2it_id"].ToString();
        }

        public void CompleteWith(Show show)
        {
            TvdbShowId = show.Id;
            ImdbId = show.ImdbId;
            Language = show.Language;
            Name = show.Name;
            Zap2ItId = show.Zap2ItID;

            SqliteManager.SaveSeries(this);

            if (show.Episodes.Count > 0) {
                SqliteManager.UpdateOrCreateSeriesEpisodes(this, show);
            }
        }

    }

}
