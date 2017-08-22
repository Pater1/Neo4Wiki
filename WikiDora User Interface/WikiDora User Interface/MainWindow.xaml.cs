using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

using NeoContainers;
using Neo4jClient;
using PageStore;

namespace WikiDoraUserInterface {

    public class PageLikedNotification : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        private bool? pageLiked = null;
        public bool? PageLiked {
            get { return pageLiked; }
            set {
                pageLiked = value;
                FieldChanged();
            }
        }

        protected void FieldChanged([CallerMemberName] string field = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(field));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private PageLikedNotification isPageLiked = new PageLikedNotification();
        private PageNode pageToDisplay = null;
        private UserWordScore pageScore = new UserWordScore();

        public MainWindow() {
            InitializeComponent();
            pageLikeDisplay.DataContext = isPageLiked;
            NextPage();
        }

        private void DislikeButton_Click(object sender, RoutedEventArgs e) {
            isPageLiked.PageLiked = false;
            pageScore.UpdateWordScores(pageToDisplay, -1);
        }

        private void LikeButton_Click(object sender, RoutedEventArgs e) {
            isPageLiked.PageLiked = true;
            pageScore.UpdateWordScores(pageToDisplay, 1);
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e) {
            isPageLiked.PageLiked = null;
            NextPage();
        }

        private void NextPage() {
            Random gen = new Random();

            // TODO: complete info about server for querying
            using (GraphClient db = new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "password")) {
                PageNode nextPageDisplay = null;
                if (pageToDisplay != null) {
                    nextPageDisplay = pageScore.GetTopScoredPage(PageNode.PullFromDatabaseByTitle(db, pageToDisplay.Title).GetLinkedPages(db));
                } else {
                    while (nextPageDisplay == null) {
                        long randId = gen.Next(5462562);
                        nextPageDisplay = PageNode.PullFromDatabaseById(db, randId);
                    }
                }
                pageToDisplay = nextPageDisplay;
                GeneratePageText();
            }
        }

        private void PageLikeDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            isPageLiked.PageLiked = null;
        }

        private void GeneratePageText() {
            pageDisplay.Children.Clear();

            XmlDocument pageText = pageToDisplay;
            XmlNode tags = pageText.FirstChild;
            XmlElement text = tags["body"];

            double regularFontSize = 16;

            foreach (XmlNode s in text.ChildNodes) {
                if (!String.IsNullOrEmpty(s.InnerText)) {
                    Label display = new Label() {
                        Content = s.InnerText,
                        FontSize = regularFontSize,
                    };

                    switch (s.Name) {
                        case "h1":
                            display.FontSize = 2 * regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;

                        case "h2":
                            display.FontSize = 1.5 * regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;

                        case "h3":
                            display.FontSize = 1.17 * regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;

                        case "h4":
                            display.FontSize = regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;

                        case "h5":
                            display.FontSize = 0.83 * regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;

                        case "h6":
                            display.FontSize = 0.67 * regularFontSize;
                            display.FontWeight = FontWeights.Bold;
                            break;
                    }
                    pageDisplay.Children.Add(display);
                }
            }
        }
    }
}
