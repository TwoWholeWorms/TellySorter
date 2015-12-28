namespace TellySorter.Models
{
    
    using System;
    using Mono.Data.Sqlite;
    
    public class Episode
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string GuessedName { get; set; }
        public int TvdbEpisodeId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public Series Series { get; set; }

        public Episode(SqliteDataReader data, Series series) : this(data)
        {
            Series = series;
        }

        public Episode(SqliteDataReader data)
        {
            Id = int.Parse(data["id"].ToString());
            Name = data["name"].ToString();
            GuessedName = data["guessed_name"].ToString();
            TvdbEpisodeId = data["tvdb_episode_id"].ToString() != "" ? int.Parse(data["tvdb_episode_id"].ToString()) : 0;
            SeasonNumber = int.Parse(data["season_number"].ToString());
            EpisodeNumber = int.Parse(data["episode_number"].ToString());
        }

        public void CompleteWith(TVDBSharp.Models.Episode episode, Series series)
        {
            TvdbEpisodeId = episode.Id;
            Name = episode.Title;
            SeasonNumber = episode.SeasonNumber;
            EpisodeNumber = episode.EpisodeNumber;

            Series = series;
            SqliteManager.SaveEpisode(this);
        }


    }

}
