using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Data.SQLite;


namespace WhatsUpOnAntyradioWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string lastId = ""; //ID from AntyRadio is as string
        const string skipId = "0000000";  //ID which should be skipped

        const int jsonMinLength = 15; // JSON string's min length
        const bool debugJsonValue = true;    //Show elements in console

        const string jsonURI = "http://rds.eurozet.pl/reader/var/antyradio.json"; //URI to json file
        //string jsonURI = "reverse.json";

        /*
         * Elements' indexes
         */
        const string id = "id";
        const string artist = "artist";
        const string title = "title";

        bool isRadioPlay = false;

        public MainWindow()
        {
            InitializeComponent();
            Errors.Visibility = Visibility.Hidden; //Turn off error message visibility

            Artist_ent.IsReadOnly = Song_ent.IsReadOnly = true;
            addToDb_btn.IsEnabled = findGoogle.IsEnabled = findYoutube.IsEnabled = false;

            antyradio.Source = new Uri("http://n-0-9.dcs.redcdn.pl/sc/o2/Eurozet/live/antyradio.livx?audio=5");
        }

        private void checkCurrentSong_Click(object sender, RoutedEventArgs e)
        {
            Errors.Visibility = Visibility.Hidden; //Turn off error message visibility

            WebClient webClient = new WebClient();

            string jsonString = webClient.DownloadString(jsonURI);    //Downloading json to string

            char[] excluded = { '}', '{' };    //Excluded chars, to split string
            string[] splitedJsonTab = jsonString.Split(excluded);   //Split json string, by excluded chars

            string json = "";
            for (int i = 0; i < splitedJsonTab.Length; i++) //Iterate to get correct json value
            {
                if (splitedJsonTab[i].Length > jsonMinLength) //Correct value is define by minimal length
                {
                    json += splitedJsonTab[i];

                    if (debugJsonValue)
                        Console.WriteLine(json);

                    if (json[json.Length - 1] == ':')   //If last element, hasn't value (caused by splitting)
                        json += "\"\""; //Adding empty element

                    break;
                }
            }

            json = "{" + json + "}";    //Encircle by brackets elements


            Dictionary<string, string> elements = null;

            try
            {
                elements = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);  //Deserialize json to dictionary
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine(ex);
                Errors.Visibility = Visibility.Visible;
                Errors.Content = "Wystapil blad. Niepowodzenie przy deserializacji.";
            }


            /*
             * Showing result, if it is correct
             */
            if (json != null && elements != null)
            {
                try
                {
                    this.lastId = elements[id].ToString(); //Assign current ID, as last got ID

                    if (skipId != this.lastId)
                    {
                        string title_s = "";
                        string artist_s = "";
                        
                        //Sometimes, could get "DOORS, The", it should be read reverse, to reach "The DOORS"
                        string[] title_arr = elements[title].Split(',');
                        string[] artist_arr = elements[artist].Split(',');

                        if (artist_arr.Length > 1)  //If this situation appeared for artist
                            for (int i = artist_arr.Length - 1; i >= 0; i--)
                                artist_s += artist_arr[i].Trim() + " ";
                        else
                            artist_s = elements[artist];

                        if (title_arr.Length > 1)  //If this situation appeared for title
                            for (int i = title_arr.Length-1; i >= 0; i--)
                                title_s += title_arr[i].Trim() + " ";
                        else
                            title_s = elements[title];


                        Song_ent.Text = title_s;
                        Artist_ent.Text = artist_s;

                        //After fill entries, turn on possibility to find in web this song and next songs
                        addToDb_btn.IsEnabled = findGoogle.IsEnabled = findYoutube.IsEnabled = true;
                    }
                    else
                    {
                        Errors.Visibility = Visibility.Visible;
                        Errors.Content = "Brak informacji o tym co leci w radio."
                                       + " \nSpróbuj ponownie za chwilę, czasem radio nieco później uaktualnia informacje o aktualnym utworze."
                                       + " \n(Możliwe, że stacja nadaje reklamy, wiadomości lub utwory, których nazwa nie jest udostępniana)";
                    }
                }
                catch (JsonWriterException ex)
                {
                    Console.WriteLine(ex);
                    Errors.Visibility = Visibility.Visible;
                    Errors.Content = "Wystapil blad. Nieznany klucz elementu.";
                }
            }
        }

        /*
         * Open on selected website in user browser
         */
        private void findInWeb_Click(object sender, RoutedEventArgs e)
        {
            string link;
            Button findBtn = (Button)sender;

            if(findBtn.Name == "findYoutube")
                link = "https://www.youtube.com/results?search_query=";
            else    //Default search in google
                link = "https://www.google.pl/search?q=";
            string queryToSearch = Artist_ent.Text.Replace(' ', '+') + "+" + Song_ent.Text.Replace(' ', '+');

            if(queryToSearch.Length > 4)
            {
                link += queryToSearch;
                Process.Start(link);
            }

        }

        /*
         * Adding result to database
         */
        private void addToDb_btn_Click(object sender, RoutedEventArgs e)
        {
            Errors.Visibility = Visibility.Hidden;

            string artist = Artist_ent.Text;
            string song = Song_ent.Text;
            int minTextLength = 3;

            if(song.Length > minTextLength || artist.Length > minTextLength)
            {
                DateTime timestamp = DateTime.Now;
                string insertQuery;

                SQLiteConnection db = new SQLiteConnection("Data Source=database.db;Version=3;");
                db.Open();

                /*
                 * check, is this new song in DB
                 */
                string selectQuery = "SELECT id FROM songs WHERE artist='" + artist + "' AND title='" + song + "';";
                SQLiteCommand cmd = new SQLiteCommand(selectQuery, db);
                SQLiteDataReader reader = cmd.ExecuteReader();

                int ilosc = 0;
                if(reader.Read())
                    ilosc++;

                /*
                 * Insert into SONGS table, new song
                 */
                if (ilosc == 0)
                {
                    insertQuery = "INSERT INTO songs VALUES ('" + this.lastId + "', '" + artist + "', '" + song + "');";
                    cmd = new SQLiteCommand(insertQuery, db);
                    cmd.ExecuteNonQuery();                   
                }

                /*
                 * Add new row, even if this same song was ealyer added
                 */
                insertQuery = "INSERT INTO saved(song_id, added_ts) VALUES ('" + this.lastId + "', '" + timestamp.ToString() + "');";
                cmd = new SQLiteCommand(insertQuery, db);
                cmd.ExecuteNonQuery();

                Errors.Visibility = Visibility.Visible;
                Errors.Content = "Dodano utwór do ulubionych ( " + timestamp + " )";
            }
            else
            {
                Errors.Visibility = Visibility.Visible;
                Errors.Content = "Nie dodano elementu do bazy, ze względu na zbyt krótką zawartość wymaganych pól";
            }
        }

        /*
         * Play Antyradio
         */
        private void ctrlRadio_btn_Click(object sender, RoutedEventArgs e)
        {
            Errors.Visibility = Visibility.Hidden;

            if (this.isRadioPlay)  //Radio is on
            {
                antyradio.Pause();
                this.isRadioPlay = false;
                ctrlRadio_btn.Content = "Graj Antyradio!";
                Errors.Visibility = Visibility.Visible;
                Errors.Content = "NIC NIE ZATRZYMA ROCK'N'ROLL-a!!! Udało Ci się go jedynie zapauzować \\m/";
            }
            else    //Radio is off / paused
            {
                antyradio.Play();
                this.isRadioPlay = true;
                ctrlRadio_btn.Content = "Zatrzymaj Rock-a";
            }
        }
    }
}
