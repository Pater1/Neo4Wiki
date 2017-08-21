using NeoContainers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageStore {
    public struct UserWordScore {
        //(-1,1)
        private Dictionary<string, double> UserWordCountScores { get; set; }

        public double this[string key] {
            get {
                if (!UserWordCountScores.ContainsKey(key)) {
                    UserWordCountScores.Add(key, 0);
                    return 0;
                }
                double x = UserWordCountScores[key]*10;
                return 2.924 * Math.Pow((x - 5), (1 / 3));
            }
        }

        private double UpdateWordScore(string word, int direction, int wordCount) {
            if (!UserWordCountScores.ContainsKey(word)) {
                UserWordCountScores.Add(word, 0.0);
            }

            double scaledDirection = direction / (double)wordCount;
            scaledDirection = 1/-(scaledDirection * scaledDirection + 1);

            Console.WriteLine(scaledDirection);

            double w = UserWordCountScores[word];
            w += scaledDirection;

            //if Math breaks, panic! (and fix the break)
            if(w >= 1) {
                w = 0.99;
            }else if(w <= -1) {
                w = -0.99;
            }

            UserWordCountScores[word] = w;
            return w;
        }

        public void UpdateWordScores(PageNode page, int direction) {
            if (direction == 0) return;
            if (direction != 1 && direction != -1) direction = Math.Sign(direction);

            foreach (string word in page.WordList) {
                UpdateWordScore(word, direction, page[word]);
            }
        }

        public double ScorePage(PageNode page) {
            double ret = 0;
            foreach (string word in page.WordList) {
                ret += this[word];
            }
            return ret;
        }

        public ScoredPage[] ScorePages(PageNode[] pages) {
            ScoredPage[] ret = new ScoredPage[pages.Length];
            for(int i = 0; i < ret.Length; i++) {
                ret[i] = new ScoredPage(ScorePage(pages[i]), pages[i]);
            }
            return ret;
        }

        public PageNode GetTopScoredPage(PageNode[] pages) {
            ScoredPage[] scores = ScorePages(pages);
            ScoredPage currentGreatest = scores[0];
            for(int i = 1; i < scores.Length; i++) {
                if(scores[i].score > currentGreatest.score) {
                    currentGreatest = scores[i];
                }
            }
            return currentGreatest.page;
        }

        public struct ScoredPage {
            public double score;
            public PageNode page;

            public ScoredPage(double s, PageNode n) {
                score = s;
                page = n;
            }
        }
    }
}
