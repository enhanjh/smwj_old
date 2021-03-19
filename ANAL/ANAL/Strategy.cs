using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace SMWJ
{
    public class Strategy
    {
        private int _tranMig;        // TRAN 이관 플래그, 0:not yet, 1:done      
        private int _retBalance;     // 예수금 조회 여부 플래그, 0:not yet, 1:done
        private int _uptBuyingAmt;   // 매수 수량 저장 여부 플래그, 0:not yet, 1:done
        private int _poolTracking;   // 풀 추적 플래그, 0:not yet, 1:done
        private int _pooling1;       // 상한가 종목 정보 조회 플래그, 0:not yet, 1:done
        private int _pooling2;       // 쌍매수(시가) 종목 정보 조회 플래그, 0:not yet, 1:done
        private int _pooling4;       // 쌍매수(종가) 종목 정보 조회 플래그, 0:not yet, 1:done
        private int _pooling5;       // 자동 추매 종목 정보 조회 플래그, 0:not yet, 1:done
        private int _pooling6;       // 볼린져밴드 하단 근접(혹은 돌파) 및 쌍매수 풀 저장 플래그, 0:not yet, 1:done
        private int _poolAvgDsp;     // 전종목 이동평균 및 이격도 조회 플래그, 0:not yet, 1:done
        private int _investor;       // 전종목 투자자 조회 플래그, 0:not yet, 1:done
        private int _refreshItemMst; // 종목마스터 최신화, 0:not yet, 1:done
        private int _calBuyAmt;      // 종목별 매수 수량 계산(쌍매수(종가)), 0:not yet, 1:done
        private int _marketIndex;    // 종합주가지수 조회, 0:not yet, 1:done
        private int _marketIndexA;   // 종합주가지수 조회(해외), 0:not yet, 1:done
        private int _marketIndexInvestor;    // 종합주가지수 투자자별 거래금액 조회, 0:not yet, 1:done

        public object _contextObj; // http response 를 위한 object

        public int _balance = 0; // 투입 가능 총 예산

        private Form1 _formObj;          // form object

        public System.Timers.Timer _timer;

        public Strategy(Form1 formObj)
        {
            this._formObj = formObj;
        }


        // 타이머
        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }


        public void StrategyStarter()
        {
            // 타이머 생성 및 시작
            _timer = new System.Timers.Timer();
            _timer.Interval = 10 * 1000; // 10초
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _timer.Start();

            // 서버기동
            AsyncServerStarter();
        }


        // http listener 기동
        public void AsyncServerStarter()
        {
            var listener = new HttpListener();

            /*
            listener.Prefixes.Add("http://localhost:8082/");
            listener.Prefixes.Add("http://127.0.0.1:8082/");
            listener.Prefixes.Add("http://10.0.1.8:8082/");
             * */
            listener.Prefixes.Add("http://*:8082/");

            listener.Start();

            while (true)
            {
                try
                {
                    var context = listener.GetContext();

                    string uri = context.Request.Url.Segments[1].Replace("/", "");

                    if (uri.Length > 0)
                    {
                        string[] temp = uri.Split(new Char[] { '&' });

                        if (temp.Length > 1)
                        {
                            ThreadPool.QueueUserWorkItem(o => HandleRequest(context, temp[0], temp[1]));
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignored for this example
                }
            }
        }


        // 요청처리
        private void HandleRequest(object state, string methodName, string param)
        {
            try
            {
                var context = (HttpListenerContext)state;

                // 관심종목 조회요청
                if (methodName.Equals(Constants.RET_INTEREST_ITEM))
                {
                    RetInterestItem(param, context);
                }
            }
            catch (Exception)
            {
                // Client disconnected or some other error - ignored for this example
            }
        }


        // 관심종목 조회
        private void RetInterestItem(string items, object context)
        {
            _contextObj = null;
            _contextObj = context;

            _formObj.Logger(Log.매도, "[관심종목 조회 시작]");

            string[] temp = items.Split(new Char[]{';'});

            _formObj._api.ApiT8407("관심종목정보요청", items, temp.Length);
        }


        // 관심종목 조회 콜백
        public void RetInterestItem_Callback(ArrayList acctItems)
        {
            _formObj.Logger(Log.매도, "[관심종목 조회 결과 http 전송]");

            string rslt = JsonConvert.SerializeObject(acctItems);

            //Console.WriteLine(rslt);

            var bytes = Encoding.UTF8.GetBytes(rslt);

            var context = (HttpListenerContext)_contextObj;

            context.Response.ContentType = "application/json; charset=UTF-8";
            context.Response.StatusCode = 200;

            context.Response.OutputStream.Write(bytes, 0, bytes.Length);

            context.Response.OutputStream.Close();

            _contextObj = null;
        }


        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            string strHhminss = DateTime.Now.ToString("HHmmss");
            int hhminss = int.Parse(strHhminss);
            string today = DateTime.Now.ToString("yyyyMMdd");

            _formObj.Logger(Log.일반, "[전략 타이머]");

            // 영업일에만 타이머대로 업무 수행
            if (_formObj._bizDayYn)
            {
                // 청산하지 못한 과거 매수 종목 업데이트
                // 풀지정일-거래일 업데이트
                if (hhminss >= 85700 && hhminss < 85730)
                {
                    if (_tranMig != 1)
                    {
                        _tranMig = 1;

                        UpdatePastBoughtItem(today);
                        UpdateTranDayCal(today);
                    }
                }
                // 오늘 예산 확인
                else if (hhminss >= 85730 && hhminss < 85800)
                {
                    if (_retBalance != 1)
                    {
                        _retBalance = 1;

                        RetAccountBalance(today);
                    }
                }
                // 오늘 시작가 확인
                // 매수 기본조건을 충족하지 못할경우 매수 대상 제외(기매수종목 조건, 이동평균 조건, 관리종목 조건)
                else if (hhminss >= 85800 && hhminss < 85925)
                {
                    RetBeginningPrice(today);
                }
                // 시작가와 예산에 맞추어 매수 수량 결정
                else if (hhminss >= 85925 && hhminss < 90000)
                {
                    if (_uptBuyingAmt != 1)
                    {
                        _uptBuyingAmt = 1; 
                        
                        UpdateBuyingAmt(today);
                    }
                }
                // 종목마스터 최신화
                else if (hhminss > 160000 && hhminss <= 184000)
                {
                    if (_refreshItemMst != 1)
                    {
                        _refreshItemMst = 1;

                        RetRefreshItemMst();
                    }
                }
                // 이동평균 및 이격도 조회
                else if (hhminss > 184000 && hhminss <= 212000)
                {
                    if (_poolAvgDsp != 1)
                    {
                        _poolAvgDsp = 1;

                        RetAverageDisparity(today);
                    }
                }
                // 투자자 조회
                else if (hhminss > 212000 && hhminss <= 235000)
                {
                    if (_investor != 1)
                    {
                        _investor = 1;

                        RetInvestor(today);
                    }
                }
                // 종합주가지수 조회
                else if (hhminss > 235000 && hhminss <= 235200)
                {
                    if (_marketIndex != 1)
                    {
                        _marketIndex = 1;

                        RetMarketIndex(today);
                    }
                }
                // 종합주가지수 조회(해외)
                else if (hhminss > 235200 && hhminss <= 235400)
                {
                    if (_marketIndexA != 1)
                    {
                        _marketIndexA = 1;

                        RetMarketIndexAbroad(today);
                    }
                }
                // 종합주가지수 투자자별 거래금액 조회
                else if (hhminss > 235200 && hhminss <= 235400)
                {
                    if (_marketIndexInvestor != 1)
                    {
                        _marketIndexInvestor = 1;

                        RetMarketIndexInvestor(today, today);
                    }
                }
                /*
                // 장중 거래 급등 주 조회
                else if (hhminss >= 100000 && hhminss <= 150000)
                {
                    int sec = int.Parse(strHhminss.Substring(4));
                    if (sec >= 35 && sec < 45)
                    {
                        RetTranAmtSpike();
                    }
                }
                // 종가 매입할 종목 발굴 : 쌍매수
                else if (hhminss > 150000 && hhminss <= 151000)
                {
                    if (_pooling4 != 1)
                    {
                        RetPoolItem4();

                        _pooling4 = 1;
                    }
                }
                // 종가 매입할 종목 수량 계산
                else if (hhminss > 151000 && hhminss <= 151300)
                {
                    if (_calBuyAmt != 1)
                    {
                        SaveBuyingListFilter(today);

                        ArrayList poolList = RetPoolItem("4");

                        double ratedBal = Math.Floor((double)(_balance * Constants.BUY_RATIO_4 / 100));

                        int[] countList = CalPriceAndAmount(poolList, Convert.ToInt32(ratedBal));

                        SavePriceAndAmount(poolList, countList, today);

                        _calBuyAmt = 1;
                    }
                } 
                // 다음날 매입할 종목 발굴 : 상한가
                else if (hhminss > 174000 && hhminss <= 175000)
                {
                    if (_pooling1 != 1)
                    {
                        RetPoolItem1(today);

                        _pooling1 = 1;
                    }
                }
                // 다음날 매입할 종목 발굴 : 쌍매수
                else if (hhminss > 175000 && hhminss <= 180000)
                {
                    if (_pooling2 != 1)
                    {
                        RetPoolItem2(today);

                        _pooling2 = 1;
                    }
                }
                // 자동 추매 종목 발굴, 5일 후 매도종목 풀저장
                else if (hhminss > 200000 && hhminss <= 200500)
                {
                    if (_pooling5 != 1)
                    {
                        RetPoolItem5(today);

                        _pooling5 = 1;
                    }
                }
                // 볼린져밴드 하단 근접(혹은 돌파) 및 쌍매수 종목 풀 저장
                else if (hhminss > 205500 && hhminss <= 210000)
                {
                    if (_pooling6 != 1)
                    {
                        RetPoolItem6(today);

                        _pooling6 = 1;
                    }
                }
                // 풀 종목 정보 추적
                else if (hhminss > 172000 && hhminss <= 174000)
                {
                    if (_poolTracking != 1)
                    {
                        _poolTracking = 1;

                        RetPoolTracking(today);
                    }
                } 
                // 프로그램 종료
                /*
                else if (hhminss >= 212000)
                {
                    _formObj.ReqAppStop();
                }
                */
            }
        }


        // 청산하지 못한 과거 매수 종목 업데이트
        public void UpdatePastBoughtItem(string today)
        {
            _formObj.Logger(Log.전략, "[청산하지 못한 과거 매수 종목 업데이트]");

            string query = "";
            query += " UPDATE TRAN";
            query += "    SET TRAN_DAY = '" + today + "'";
            query += "  WHERE TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
            query += "                      FROM (SELECT * FROM TRAN) A";
            query += "                     WHERE A.TRAN_DAY < '" + today + "'";
            query += "                   )";
            query += "    AND TRAN_SP  = 2";
            query += "    AND SELL_ORDER_NO IS NULL";

            _formObj._db.InsertQuery(query);
        }


        // 풀지정일-거래일 업데이트
        public void UpdateTranDayCal(string today)
        {
            _formObj.Logger(Log.전략, "[풀지정일-거래일 업데이트]");

            string query = "";
            query += " INSERT ";
            query += "   INTO TRAN_DAY_CAL";
            query += "      ( SEQ";
            query += "      , POOL_DAY";
            query += "      , TRAN_DAY";
            query += "      , WORK_MAN";
            query += "      , WORK_TIME";
            query += "      )";
            query += " SELECT MAX(A.SEQ)+1";
            query += "      , (SELECT MAX(AA.TRAN_DAY) FROM TRAN_DAY_CAL AA WHERE AA.TRAN_DAY < '" + today + "')";
            query += "      , '" + today + "'";
            query += "      , 'M'";
            query += "      , NOW()";
            query += "   FROM TRAN_DAY_CAL A";

            _formObj._db.InsertQuery(query);
        }


        // 계좌 잔고(예수금) 조회
        public void RetAccountBalance(string today)
        {
            string query = "";
            query += " SELECT A.BUDGET_AMT AS AMT";
            query += "   FROM BUDGET A";

            query += "  UNION ALL";

            query += " SELECT SUM(C.BUY_AMT)";
            query += "   FROM TRAN C";
            query += "  WHERE C.TRAN_DAY = '" + today + "'";
            query += "    AND C.TRAN_SP  = 2";
            query += "    AND C.MODE    != '99'";

            ArrayList bal = _formObj._db.SelectQuery(query);
            Dictionary<string, string> amt = (Dictionary<string, string>)bal[0];
            Dictionary<string, string> buy = (Dictionary<string, string>)bal[1];

            //_balance = (int)((Int32.Parse(amt["AMT"]) - Int32.Parse(buy["AMT"])) / 1.3); // 시장가 매수를 위해서 시초가 1.3배 정도의 잔고가 필요함
            _balance = (int)((Int32.Parse(amt["AMT"]) - Int32.Parse(buy["AMT"])) / 1.3) + 1000000; // 임시조정...

            _formObj.Logger(Log.전략, "[계좌 잔고 조회 시작, 매수 비율 계산 시작] DB 예수금 : {0}", _balance);
        }


        // 근접 시작가 조회
        public void RetBeginningPrice(string today)
        {
            // 일자 조건 : 오늘을 제외한 가장 최신의 풀 종목을 조회함
            // 기매수종목 조건 : 이미 매수 요청한 종목은 제외함, 과거 매수한 종목 중 청산하지 못한 종목은 제외함
            // 이동평균 조건 : 5일이동평균 > 10일이동평균, 10일이동평균 > 20일이동평균
            // 관리종목 조건 : 경고, 위험 종목은 제외
            string query = "";
            query += " SELECT A.MODE";
            query += "      , A.ITEM";
            query += "      , A.TRAN_DAY";
            query += "   FROM POOL A";
            query += "      , PRICE B";
            query += "      , ITEM C";
            query += "  WHERE A.ITEM     = B.ITEM";
            query += "    AND A.TRAN_DAY = B.TRAN_DAY";
            query += "    AND A.ITEM     = C.ITEM";
            query += "    AND A.TRAN_DAY = (SELECT MAX(TRAN_DAY)";
            query += "                        FROM POOL";
            query += "                       WHERE TRAN_DAY < '" + today + "'";
            query += "                     )";
            query += "    AND A.ITEM NOT IN (SELECT AA.ITEM";
            query += "                         FROM TRAN AA";
            query += "                        WHERE AA.TRAN_DAY = '" + today + "'";
            query += "                      )";
            query += "    AND B.AVG_5 > B.AVG_10";
            query += "    AND B.AVG_10 > B.AVG_20";
            query += "    AND C.WARN   != 'Y'";
            query += "    AND C.DANGER != 'Y'";
            query += "    AND A.MODE    = 7";

            //query += "  UNION ALL";

            // 자동추매 대상 종목은 기매수종목이어야 함
            //query += " SELECT A.MODE";
            //query += "      , A.ITEM";
            //query += "      , A.TRAN_DAY";
            //query += "   FROM POOL A";
            //query += "      , PRICE B";
            //query += "      , ITEM C";
            //query += "  WHERE A.ITEM     = B.ITEM";
            //query += "    AND A.TRAN_DAY = B.TRAN_DAY";
            //query += "    AND A.ITEM     = C.ITEM";
            //query += "    AND A.TRAN_DAY = (SELECT MAX(TRAN_DAY)";
            //query += "                        FROM POOL";
            //query += "                       WHERE TRAN_DAY < '" + today + "'";
            //query += "                     )";
            //query += "    AND A.ITEM    IN (SELECT AA.ITEM";
            //query += "                        FROM TRAN AA";
            //query += "                        WHERE AA.TRAN_DAY = '" + today + "'";
            //query += "                      )";
            ////query += "    AND B.AVG_5 > B.AVG_10";
            ////query += "    AND B.AVG_10 > B.AVG_20";
            //query += "    AND C.WARN   != 'Y'";
            //query += "    AND C.DANGER != 'Y'";
            //query += "    AND A.MODE    = 5";

            //query += "  UNION ALL";

            // 볼린져밴드 하단 및 쌍매수 조건은 부가조건 고려하지 않음
            //query += " SELECT A.MODE";
            //query += "      , A.ITEM";
            //query += "      , A.TRAN_DAY";
            //query += "   FROM POOL A";
            //query += "      , ITEM C";
            //query += "  WHERE A.ITEM     = C.ITEM";
            //query += "    AND A.TRAN_DAY = (SELECT MAX(TRAN_DAY)";
            //query += "                        FROM POOL";
            //query += "                       WHERE TRAN_DAY < '" + today + "'";
            //query += "                     )";
            //query += "    AND A.ITEM NOT IN (SELECT AA.ITEM";
            //query += "                         FROM TRAN AA";
            //query += "                        WHERE AA.TRAN_DAY = '" + today + "'";
            //query += "                      )";
            //query += "    AND C.WARN   != 'Y'";
            //query += "    AND C.DANGER != 'Y'";
            //query += "    AND A.MODE    = 6";

            ArrayList pool = _formObj._db.SelectQuery(query);

            // 종목코드 중복을 제거함
            StringBuilder items = new StringBuilder();
            int cnt = 0;
            foreach (Dictionary<string, string> row in pool)
            {
                string itemCd = row["ITEM"];
                if (!((items.ToString()).Contains(itemCd)))
                {
                    items.Append(itemCd + ";");
                    cnt++;
                }
            }

            _formObj.Logger(Log.전략, "[근접 시작가 조회 시작] 종목건수 : {0}", cnt);

            _formObj._api.ApiT8407hoga("근접시작가조회", items.ToString(), cnt);
        }


        /*
        // 근접 시작가 조회_openApi로부터 콜백
        public void RetBeginningPrice_Callback(ArrayList list)
        {
            _formObj.Logger(Log.전략, "[근접 시작가 조회 콜백] 건수 : {0}", list.Count);
            
            string query = "";

            if (mode == "2")
            {
                // 이격도5 가 105 이상인 종목만 매수
                query += " UPDATE POOL A";
                query += "    SET A.BEGIN_PRICE         = '" + price + "'";
                query += "      , A.BUY_INVOLVE_ITEM_YN = IF( (SELECT AAA.DSP_5";
                query += "                                       FROM PRICE AAA";
                query += "                                      WHERE AAA.TRAN_DAY = A.TRAN_DAY";
                query += "                                        AND AAA.ITEM     = A.ITEM";
                query += "                                    ) >= 104, 'Y', 'N')";
                query += "      , A.BEGINPRICE_TIME     = NOW()";
                query += "  WHERE A.TRAN_DAY = (SELECT MAX(AA.TRAN_DAY)";
                query += "                        FROM (SELECT * FROM POOL) AA";
                query += "                       WHERE AA.TRAN_DAY < '" + today + "'";
                query += "                   )";
                query += "    AND A.ITEM     = '" + item + "'";
                query += "    AND A.MODE     = '" + mode + "'";
            }
            else if (mode == "5")
            {
                double ratedBal = Math.Floor((double)(_balance * Constants.BUY_RATIO_5 / 100));
                if(ratedBal > 1000000) 
                {
                    ratedBal = 1000000;
                }

                double cnt = Math.Round(ratedBal / price);

                // 자동 추매 대상 종목은 근접 시작가 조회 종료 후, 매수포함종목여부 결정
                query += " UPDATE POOL";
                query += "    SET PRICE           = ROUND((SELECT (AA.BUY_TOT + " + (cnt * price) + ") / (AA.BUY_CNT + " + cnt + ")";
                query += "                                   FROM TRAN AA";
                query += "                                  WHERE AA.ITEM     = '" + item + "'";
                query += "                                    AND AA.TRAN_DAY = '" + today + "'";
                query += "                                    AND AA.TRAN_SP  = 2";                
                query += "                               ), 0)";
                query += "      , BEGIN_PRICE     = '" + price + "'";
                query += "      , BEGINPRICE_TIME = NOW()";
                query += "  WHERE TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
                query += "                      FROM (SELECT * FROM POOL) A";
                query += "                     WHERE A.TRAN_DAY < '" + today + "'";
                query += "                   )";
                query += "    AND ITEM     = '" + item + "'";
                query += "    AND MODE     = '" + mode + "'";
            }
            else if (mode == "6" || mode == "7")
            {
                // 제약조건 없음
                query += " UPDATE POOL";
                query += "    SET BEGIN_PRICE         = '" + price + "'";
                query += "      , BUY_INVOLVE_ITEM_YN = 'Y'";
                query += "      , BEGINPRICE_TIME     = NOW()";
                query += "  WHERE TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
                query += "                      FROM (SELECT * FROM POOL) A";
                query += "                     WHERE A.TRAN_DAY < '" + today + "'";
                query += "                   )";
                query += "    AND ITEM     = '" + item + "'";
                query += "    AND MODE     = '" + mode + "'";
            }
            else
            {
                // 근접 시작가가 전일 종가보다 5% 초과할 경우, 매수하지 않음
                query += " UPDATE POOL";
                query += "    SET BEGIN_PRICE         = '" + price + "'";
                query += "      , BUY_INVOLVE_ITEM_YN = IF( ( " + price + " / PRICE ) > 1.05, 'N', 'Y')";
                query += "      , BEGINPRICE_TIME     = NOW()";
                query += "  WHERE TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
                query += "                      FROM (SELECT * FROM POOL) A";
                query += "                     WHERE A.TRAN_DAY < '" + today + "'";
                query += "                   )";
                query += "    AND ITEM     = '" + item + "'";
                query += "    AND MODE     = '" + mode + "'";
            }
        }
        */

        // 시작가와 예산에 맞추어 매수 수량 결정
        public void UpdateBuyingAmt(string today) 
        {
            // 전략 1, 2
            ArrayList poolList = RetPoolItem("1, 2");

            double ratedBal = Math.Floor((double)(_balance * Constants.BUY_RATIO_1_2 / 100));

            int[] countList = CalPriceAndAmount(poolList, Convert.ToInt32(ratedBal));

            SavePriceAndAmount(poolList, countList, today);

            // 전략 7
            ArrayList poolList7 = RetPoolItem("7");

            double ratedBal7 = Math.Floor((double)(_balance * Constants.BUY_RATIO_7 / 100));

            int[] countList7 = CalPriceAndAmount(poolList7, Convert.ToInt32(ratedBal7));

            SavePriceAndAmount(poolList7, countList7, today);
        }


        // 매수할 종목 정보 조회
        private ArrayList RetPoolItem(string mode)
        {
            string query = "";
            query += " SELECT A.MODE";
            query += "      , A.TRAN_DAY";
            query += "      , A.ITEM";
            query += "      , A.PRICE";
            query += "      , B.CNT";
            query += "      , IFNULL(C.AMT, 0) AS AMT";
            query += "   FROM (";
            query += "         SELECT A.MODE";
            query += "              , A.ITEM";
            query += "              , A.TRAN_DAY";
            query += "              , IF(A.BEGIN_PRICE = 0, A.PRICE, A.BEGIN_PRICE) AS PRICE"; // 종가 매수할 때는 PRICE, 시가 매수할 때는 BEGIN_PRICE
            query += "           FROM POOL A";
            query += "              , STRATEGY B";
            query += "          WHERE A.MODE                = B.MODE";
            query += "            AND B.USE_YN              = 'Y'";
            query += "            AND A.BUY_INVOLVE_ITEM_YN = 'Y'";
            query += "            AND TRAN_DAY              = (SELECT DISTINCT MAX(TRAN_DAY) FROM POOL)";
            query += "            AND A.MODE               IN (" + mode + ")";
            query += "        ) A";
            query += "      , (";
            query += "         SELECT COUNT(*) AS CNT";
            query += "           FROM POOL A";
            query += "              , STRATEGY B";
            query += "          WHERE A.MODE   = B.MODE";
            query += "            AND B.USE_YN = 'Y'";
            query += "            AND TRAN_DAY = (SELECT DISTINCT MAX(TRAN_DAY) FROM POOL)";
            query += "            AND A.MODE  IN (" + mode + ")";
            query += "        ) B";
            query += "      , (";
            query += "         SELECT BUDGET_AMT AS AMT";
            query += "           FROM BUDGET";
            query += "        ) C";
            query += "  ORDER BY PRICE DESC";

            ArrayList poolList = _formObj._db.SelectQuery(query);           
            
            return poolList;
        }


        // 매수 비율 계산
        private int[] CalPriceAndAmount(ArrayList poolList, int budget)
        {
            int handleBudget = budget;
		    int spentBudget = 0;
	        int nextPoolCnt = poolList.Count;
		    int itemCnt = 0;

            // 종목당 매수 가능 금액을 10만원 이하로 제한
            if (handleBudget > (nextPoolCnt * 100000))
            {
                handleBudget = (nextPoolCnt * 100000);
            }

            int[] priceList = new int[poolList.Count];
            int[] countList = new int[poolList.Count];

            int poolIndex = 0;
            foreach(Dictionary<string, string> row in poolList)
            {
                priceList[poolIndex++] = Convert.ToInt32( row["PRICE"] );
            }

            ArrayList nextPriceList = new ArrayList();
		    ArrayList tempPriceList = new ArrayList();
		    
            int whileCnt = 0;
		
		    while(true){

                _formObj.Logger(Log.전략, "**********************while문 시작**********************");
			    _formObj.Logger(Log.전략, "nextPoolCnt :" + nextPoolCnt);
			
			    for(int i = 0 ; i < priceList.Length; i++ )
                {
				    if( whileCnt == 0 )
                    {
                        double budPerCnt = handleBudget / nextPoolCnt;
					    int unitBudget = (int) Math.Ceiling( budPerCnt );

                        _formObj.Logger(Log.전략, "종목당 매수 가능 금액 :" + unitBudget);
					    _formObj.Logger(Log.전략, "매수가 : " + priceList[i]);

                        int cnt = (int) (unitBudget / (priceList[i] * (1 + Constants.FEE)));
					
                        _formObj.Logger(Log.전략, "매수수량 :" + cnt);

                        countList[i] = cnt + countList[i];
					
                        _formObj.Logger(Log.전략, "매수수량 합계 :" + countList[i]);

					    int unitSpentBudget = (int) Math.Round(cnt * priceList[i] * ( 1 + Constants.FEE ));
					    
                        _formObj.Logger(Log.전략, "매수 투입한 금액 :" + unitSpentBudget);

					    spentBudget = spentBudget + unitSpentBudget;
					
					    if(cnt > 0 )
                        {
						    itemCnt = itemCnt + 1;
						    tempPriceList.Add(priceList[i]);
					    }
				    }
                    else
                    {
					    for( int j = 0 ; j < nextPriceList.Count; j++ )
                        {
						    if( ((int) nextPriceList[j]) == priceList[i] )
                            {
                                double budPerCnt = handleBudget / nextPoolCnt;
							    int unitBudget = (int) Math.Ceiling( budPerCnt );

                                _formObj.Logger(Log.전략, "종목당 매수 가능 금액 :" + unitBudget);
							    _formObj.Logger(Log.전략, "매수가 : " + priceList[i]);

                                int cnt = (int)(unitBudget / (priceList[i] * (1 + Constants.FEE)));
							
                                _formObj.Logger(Log.전략, "매수수량 :" + cnt);

                                countList[i] = cnt + countList[i];
							
                                _formObj.Logger(Log.전략, "매수수량 합계 :" + countList[i]);
		
							    int unitSpentBudget = (int) Math.Round(cnt * priceList[i] * ( 1 + Constants.FEE));
							    
                                _formObj.Logger(Log.전략, "매수 투입한 금액 :" + unitSpentBudget);
		
							    spentBudget = spentBudget + unitSpentBudget;
							
							    if(cnt > 0 )
                                {
								    itemCnt = itemCnt + 1;
								    tempPriceList.Add(priceList[i]);
							    }
						    }
					    }
				    }
			    }

			    _formObj.Logger(Log.전략, "itemCnt :" + itemCnt);

			    handleBudget = handleBudget - spentBudget;
			    nextPoolCnt = itemCnt;
                spentBudget = 0;
			    whileCnt = whileCnt + 1;			    
			    itemCnt = 0;
			    nextPriceList = tempPriceList;
			    tempPriceList = new ArrayList();
			    _formObj.Logger(Log.전략, "--------------------");
			    _formObj.Logger(Log.전략, "handleBudget :" + handleBudget);

			    if( nextPriceList.Count== 0 )
                {
				    break;
			    }
		    }

            return countList;
        }


        // 매수 비율 업데이트
        private void SavePriceAndAmount(ArrayList poolList, int[] countList, string today)
        {
            string query = "";
            for (int i = 0; i < poolList.Count; i++)
            {
                Dictionary<string, string> pool = (Dictionary<string, string>)poolList[i];
                string mode = pool["MODE"];
                
                int cnt       = (int) countList[i];
                int tranAmt   = cnt * int.Parse(pool["PRICE"]);
                int fee       = (int) Math.Ceiling(tranAmt * Constants.FEE);
                int adjustAmt = tranAmt + (int) Math.Ceiling(tranAmt * Constants.FEE);

                query += " UPDATE POOL";
                query += "    SET CNT        = " + cnt;
                query += "      , TRAN_AMT   = " + tranAmt;
                query += "      , ADJUST_AMT = " + adjustAmt;
                query += "      , FEE        = " + fee;
                query += "      , RATE_TIME  = NOW()";
                query += "  WHERE MODE       = " + mode;
                query += "    AND TRAN_DAY   = '" + pool["TRAN_DAY"] + "'";
                query += "    AND ITEM       = '" + pool["ITEM"] + "'";

                _formObj._db.InsertQuery(query);
                query = "";
            }

            double ratedBal = Math.Floor((double)(_balance * Constants.BUY_RATIO_5 / 100));
            if (ratedBal > 1000000)
            {
                ratedBal = 1000000;
            }

            // 자동 추매 대상 종목의 매수 수량을 업데이트
            query = "";
            query += " UPDATE POOL";
            query += "    SET RATE       = ROUND((BEGIN_PRICE - PRICE) / PRICE * 100, 2)";
            query += "      , CNT        = ROUND(" + ratedBal + " / PRICE, 0)";
            query += "      , RATE_TIME  = NOW()";
            query += "  WHERE MODE       = 5";
            query += "    AND TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
            query += "                      FROM (SELECT * FROM POOL) A";
            query += "                     WHERE A.TRAN_DAY < '" + today + "'";
            query += "                   )";

            _formObj._db.InsertQuery(query);

            // 자동 추매 대상 종목의 매수 여부 최종 확정
            query = "";
            query += " UPDATE POOL";
            query += "    SET BUY_INVOLVE_ITEM_YN = 'Y'";
            query += "  WHERE MODE       = 5";
            query += "    AND TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
            query += "                      FROM (SELECT * FROM POOL) A";
            query += "                     WHERE A.TRAN_DAY < '" + today + "'";
            query += "                   )";
            query += "    AND RATE BETWEEN 0 AND 1";
            query += "  ORDER BY RATE ASC LIMIT 1";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[매수 수량 저장 종료]");
        }


        // 종가 매수 종목 필터(관리종목, 이동평균 조건 적용)
        private void SaveBuyingListFilter(string today)
        {
            string query = "";
            query += " UPDATE POOL";
            query += "    SET BUY_INVOLVE_ITEM_YN = 'Y'";
            query += "      , RATE_TIME           = NOW()";
            query += "  WHERE MODE       = 4";
            query += "    AND TRAN_DAY   = '" + today + "'";
            query += "    AND ITEM      IN (SELECT AA.ITEM";
            query += "                        FROM ITEM AA";
            query += "                           , PRICE BB";
            query += "                       WHERE AA.ITEM     = BB.ITEM";
            query += "                         AND BB.TRAN_DAY = (SELECT MAX(TRAN_DAY) FROM PRICE)";
            query += "                         AND BB.AVG_5    > BB.AVG_10";
            query += "                         AND BB.AVG_10   > BB.AVG_20";
            query += "                         AND AA.WARN    != 'Y'";
            query += "                         AND AA.DANGER  != 'Y'";
            query += "                     )";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[종가 매수 종목 필터 완료]");
        }

        /*
        // 거래량 급등 종목 조회
        public void RetTranAmtSpike()
        {
            Dictionary<string, string> param = new Dictionary<string, string>();

            param.Add("시장구분", "000"); // 000: 전체, 001:코스피, 101:코스닥
            param.Add("정렬구분", "2"); // 1: 급증량, 2:급증률
            param.Add("시간구분", "1"); // 1: 분, 2: 전일            
            param.Add("거래량구분", "50"); // 5: 5천주이상, 10: 만주이상, 50: 5만주이상, 100: 10만주이상, 200: 20만주이상, 300: 30만주이상, 500: 50만주이상, 1000: 백만주이상
            param.Add("시간", "1"); // 분 입력
            param.Add("종목조건", "1"); // 0: 전체조회, 1: 관리종목제외
            param.Add("가격구분", "8"); // 0: 전체조회, 2: 5만원이상, 5: 1만원이상, 6: 5천원이상, 8: 1천원이상, 9: 10만원이상

            _formObj.OpenApiRequest("거래량급증요청", "OPT10023", 0, Constants.RET_TRAN_AMT_SPIKE, param);

            _formObj.Logger(Log.전략, "[거래량 급등 종목 조회 시작]");
        }


        // 거래량 급등 종목 조회 콜백
        public void RetTranAmtSpike_Callback(string item, int price, string hhmm)
        {
            _tranAmtSpikeItem = _tranAmtSpikeItem + item + ";";

            _formObj.Logger(Log.전략, "[거래량 급등 종목 조회 콜백] 종목코드 : {0}, 현재가 : {1}", item, price);
            
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("종목코드", item);
            param.Add("틱범위", "1"); // 틱범위 1분
            param.Add("수정주가구분", "0"); // 수정주가아님

            _formObj.OpenApiRequest(item + ";" + hhmm, "OPT10080", 0, Constants.RET_TRAN_AMT_SPIKE, param);
            _formObj.Logger(Log.전략, "[거래량 급등 종목 매수 고려 시작]");
        }


        // 외인 쌍매수(종가) 풀 조회
        public void RetPoolItem4()
        {
            Dictionary<string, string> param = new Dictionary<string, string>();

            param.Add("매매구분", "1");     // 1:순매수, 2:순매도
            param.Add("시장구분", "000");   // 000: 전체, 001: 코스피, 101: 코스닥        	
            param.Add("기관구분", "9100");  // 9000:외국인, 9100:외국계, 1000:금융투자, 3000:투신, 5000:기타금융, 4000:은행, 2000:보험, 6000:연기금, 7000:국가, 7100:기타법인, 9999:기관계

            _formObj.OpenApiRequest("장중투자자별매매상위요청-9100", "OPT10065", 0, Constants.RET_DUAL_PULLING_E, param);

            _formObj.Logger(Log.전략, "[외인 쌍매수(종가) 풀 조회 시작]");
        }


        // 상한가 풀 조회
        public void RetPoolItem1(string yyyymmdd)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            //param.Add("시장구분", "000");
            //param.Add("상하한구분", "1");
            //param.Add("정렬구분", "1");
            //param.Add("종목조건", "10");
            //param.Add("거래량구분", "00000");
            //param.Add("신용조건", "0");
            //param.Add("매매금구분", "8");


            param.Add("시장구분", "000"); // 000: 전체, 001: 코스피, 101: 코스닥
            param.Add("상하한구분", "1"); // 1: 상한, 2: 상승, 3: 보합, 4: 하한, 5: 하락, 6: 전일상한, 7:전일하한
            param.Add("정렬구분", "1"); // 1:종목코드순, 2:연속횟수순(상위100개), 3:등락률순
            param.Add("종목조건", "0"); // 0:전체조회,1:관리종목제외, 3:우선주제외, 4:우선주+관리종목제외, 5:증100제외, 6:증100만 보기, 7:증40만 보기, 8:증30만 보기, 9:증20만 보기, 10:우선주+관리종목+환기종목제외
            param.Add("거래량구분", "00000"); // 00000:전체조회, 00010:만주이상, 00050:5만주이상, 00100:10만주이상, 00150:15만주이상, 00200:20만주이상, 00300:30만주이상, 00500:50만주이상, 01000:백만주이상
            param.Add("신용조건", "0"); // 0:전체조회, 1:신용융자A군, 2:신용융자B군, 3:신용융자C군, 4:신용융자D군, 9:신용융자전체
            param.Add("매매금구분", "0"); // 0:전체조회, 1:1천원미만, 2:1천원~2천원, 3:2천원~3천원, 4:5천원~1만원, 5:1만원이상, 8:1천원이상

            _formObj.OpenApiRequest("상하한가요청-" + yyyymmdd, "OPT10017", 0, Constants.RET_ULP.ToString(), param);

            _formObj.Logger(Log.전략, "[상한가 풀 조회 시작]");
        }


        // 전일-당일 거래량 비교 콜백
        public void CompTranAmt_Callback(string item, string price, string ratio, string tranDay, string mode)
        {
            _formObj.Logger(Log.전략, "[전일-당일 거래량 비교 콜백] 종목코드-현재가-등락율-일자-전략 : " + item + "-" + price + "-" + ratio + "-" + tranDay + "-" + mode);

            string query = "";
            query += " INSERT INTO POOL";
            query += " (MODE, TRAN_DAY, ITEM, PRICE, RATE, POOL_TIME)";
            query += " VALUES";
            query += " (" + mode + ", '" + tranDay + "', '" + item + "', '" + price + "', '" + ratio + "', NOW())";
            query += " ON DUPLICATE KEY UPDATE PRICE     = '" + price + "'";
            query += "                       , RATE      = '" + ratio + "'";
            query += "                       , POOL_TIME = NOW()";

            _formObj._db.InsertQuery(query);
        }


        // 풀 종목 추적
        public async void RetPoolTracking(string today)
        {
            string query = "";
            query += " SELECT ITEM";
            query += "      , TRAN_DAY";
            query += "      , MODE";
            query += "   FROM TRAN";
            query += "  WHERE TRAN_DAY = '" + today + "'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                _poolTrackingFlag = false;

                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("종목코드", row["ITEM"]);
                param.Add("틱범위", "1"); // 틱범위 1분
                param.Add("수정주가구분", "0"); // 수정주가아님

                _formObj.Logger(Log.전략, "[풀 추적 조회 시작] 종목코드 : {0}", param["종목코드"]);

                _formObj.OpenApiRequest(row["TRAN_DAY"] + "-" + param["종목코드"] + "-" + row["MODE"], "OPT10080", 0, Constants.RET_POOL_TRACKING, param);

                _poolTrackingFlag = await Task<bool>.Run(() => RetFlagPoolTracking());
            }
        }


        // 콜백이 완료될 때까지 기다림
        private bool RetFlagPoolTracking()
        {
            while (!_poolTrackingFlag) {}

            return _poolTrackingFlag;
        }


        // 풀 추적 조회 콜백
        public void RetPoolTracking_Callback(string today, int startPrice, int endPrice, int highPrice, string highHhmin, int lowPrice, string lowHhmin, string item, string mode, int ydayPrice)
        {
            string profitYn = "N";
            if (startPrice < highPrice)
            {
                profitYn = "Y";
            }

            if (startPrice == 0 || endPrice == 0 || highPrice == 0 || lowPrice == 0)
            {
                highHhmin = "19770101000000";
                lowHhmin = "19770101000000";
                profitYn = "E";
            }

            string query = "";
            query += " INSERT INTO TRACKING";
            query += "        (MODE, ITEM, TRAN_DAY, PRICE, NEXT_DAY, START_PRICE, DIFF_YEND_START_PRICE, DIFF_YEND_START_RATE, HIGH_PRICE, HIGH_PRICE_TIME, DIFF_START_HIGH_PRICE, DIFF_START_HIGH_RATE, LOW_PRICE, LOW_PRICE_TIME, DIFF_START_LOW_PRICE, DIFF_START_LOW_RATE, END_PRICE, PROFIT_YN)";
            query += " SELECT MODE";
            query += "      , ITEM";
            query += "      , SUBSTR(TRAN_ID, 1, 8)"; // 최초 매수일을 기준으로 tran_day를 저장
            query += "      , " + ydayPrice;
            query += "      , '" + today + "'";
            query += "      , " + startPrice;
            query += "      , " + startPrice + " - " + ydayPrice;
            query += "      , ROUND((" + startPrice + " - " + ydayPrice + ") / " + ydayPrice + " * 100, 2)";
            query += "      , " + highPrice;
            query += "      , '" + highHhmin + "'";
            query += "      , " + highPrice + " - " + startPrice;
            query += "      , ROUND((" + highPrice + " - " + startPrice + ") / " + startPrice + " * 100, 2)";
            query += "      , " + lowPrice; 
            query += "      , '" + lowHhmin + "'";
            query += "      , " + lowPrice + " - " + startPrice;
            query += "      , ROUND((" + lowPrice + " - " + startPrice + ") / " + startPrice + " * 100, 2)";
            query += "      , " + endPrice;
            query += "      , IF(SUBSTR(TRAN_ID, 1, 8) = TRAN_DAY, '" + profitYn + "', '')";
            query += "   FROM TRAN";
            query += "  WHERE ITEM     = '" + item + "'";
            query += "    AND MODE     = '" + mode + "'";
            query += "    AND TRAN_DAY = '" + today + "'";
            query += "    AND TRAN_SP IN (2, 4)";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[풀 추적 조회 콜백] 종목코드 : {0}, 현재가 : {1}", item, endPrice);
        }
        */

        // 종목마스터 최신화
        public void RetRefreshItemMst()
        {
            _formObj._api.ApiT8430("종목마스터조회");
        }


        // 종목코드 리스트 조회 콜백
        public void RetRefreshItemMst_Callback()
        {
            string query = "";
            query += " SELECT ITEM";
            query += "   FROM ITEM";
            query += "  WHERE ITEM_SP IN ('P', 'D')";
            query += "    AND USE_YN   = 'Y'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                string item = row["ITEM"];

                _formObj.Logger(Log.전략, "[시가총액 및 재무정보 조회] 종목코드 : {0}", item);

                _formObj._api.ApiT3320("시가총액 및 재무정보 조회", item);
            }
        }


        // 이동평균 및 이격도 조회(당일)
        public void RetAverageDisparity(string yyyymmdd)
        {
            string query = "";
            query += " SELECT ITEM";
            query += "   FROM ITEM";
            query += "  WHERE ITEM_SP IN ('P', 'D')";
            query += "    AND USE_YN   = 'Y'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                string item = row["ITEM"];

                _formObj.Logger(Log.전략, "[이동평균 및 이격도 조회 시작] 종목코드 : {0}", item);

                _formObj._api.ApiT8413("price 조회", item, yyyymmdd, yyyymmdd);
            }
        }


        // 이동평균 및 이격도 조회(기간)
        public void RetAverageDisparityLong(string sdt, string edt)
        {
            string query = "";
            query += " SELECT ITEM";
            query += "   FROM ITEM";
            query += "  WHERE ITEM_SP IN ('P', 'D')";
            query += "    AND USE_YN   = 'Y'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                string item = row["ITEM"];

                _formObj.Logger(Log.전략, "[이동평균 및 이격도 조회(기간) 시작] 종목코드 : {0}", item);

                _formObj._api.ApiT8413("price 조회", item, sdt, edt);
            }
        }


        // 자동 추매 풀 조회, 5일후 매도 종목 풀저장
        public void RetPoolItem5(string today)
        {
            string query = "";
            query += " INSERT INTO POOL";
            query += " (MODE, TRAN_DAY, ITEM, PRICE, RATE, POOL_TIME)";
            query += " SELECT 5 AS MODE";
            query += "      , A.TRAN_DAY";
            query += "      , A.ITEM";
            query += "      , B.END";
            query += "      , NULL";
            query += "      , NOW()";
            query += "   FROM TRAN A";
            query += "      , PRICE B";
            query += "      , ITEM C";
            query += "  WHERE A.ITEM     = B.ITEM";
            query += "    AND A.TRAN_DAY = B.TRAN_DAY";
            query += "    AND A.ITEM     = C.ITEM";
            query += "    AND A.MODE     IN (1, 2, 3, 4)";
            query += "    AND A.TRAN_DAY = '" + today + "'";
            query += "    AND A.TRAN_SP  = 2";
            query += "    AND B.END      < B.BOL_BTM";
            //query += "    AND B.AVG_5    > B.AVG_10";
            //query += "    AND B.AVG_10   > B.AVG_20";
            query += "    AND C.WARN    != 'Y'";
            query += "    AND C.DANGER  != 'Y'";
            query += "    AND A.BUY_TOT <= 150000";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[자동 추매 종목 저장 완료]");


            query = "";
            query += " INSERT INTO POOL";
            query += " (MODE, TRAN_DAY, ITEM, PRICE, RATE, POOL_TIME)";
            query += " SELECT 7 AS MODE";
            query += "      , A.TRAN_DAY";
            query += "      , A.ITEM";
            query += "      , A.PRICE";
            query += "      , A.RATE";
            query += "      , NOW()";
            query += "   FROM POOL A";
            query += "      , PRICE B";
            query += "  WHERE A.ITEM     = B.ITEM";
            query += "    AND A.TRAN_DAY = B.TRAN_DAY";
            query += "    AND A.TRAN_DAY = '" + today + "'";
            query += "    AND A.MODE     = 2";
            query += "    AND B.AVG_5 > B.AVG_10";
            query += "    AND B.AVG_10 > B.AVG_20";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[5일후 매도 종목 풀저장 완료]");
        }


        // 종목별투자자(당일)
        public void RetInvestor(string yyyymmdd)
        {
            string query = "";
            query += " SELECT ITEM";
            query += "   FROM ITEM";
            query += "  WHERE ITEM_SP IN ('P', 'D')";
            query += "    AND USE_YN   = 'Y'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                string item = row["ITEM"];

                _formObj.Logger(Log.전략, "[종목별투자자 조회 시작] 종목코드 : {0}", item);

                _formObj._api.ApiT1717("종목별투자자조회", item, yyyymmdd, yyyymmdd);
            }
        }

        
        // 종목별투자자(기간)
        public void RetInvestorLong(string sdt, string edt)
        {
            string query = "";
            query += " SELECT ITEM";
            query += "   FROM ITEM";
            query += "  WHERE ITEM_SP IN ('P', 'D')";
            query += "    AND USE_YN   = 'Y'";

            ArrayList rslt = _formObj._db.SelectQuery(query);

            foreach (Dictionary<string, string> row in rslt)
            {
                string item = row["ITEM"];

                _formObj.Logger(Log.전략, "[종목별투자자 조회(기간) 시작] 종목코드 : {0}", item);

                _formObj._api.ApiT1717("종목별투자자조회", item, sdt, edt);
            }
        }


        // 볼린져밴드 하단 근접(혹은 돌파) 및 쌍매수
        public void RetPoolItem6(string today)
        {
            string query = "";
            query += " INSERT INTO POOL";
            query += " (MODE, TRAN_DAY, ITEM, PRICE, RATE, POOL_TIME)";
            query += " SELECT 6 AS MODE";
            query += "      , A.TRAN_DAY";
            query += "      , A.ITEM";
            query += "      , A.END";
            query += "      , NULL";
            query += "      , NOW()";
            query += "   FROM PRICE A";
            query += "      , INVESTOR B";
            query += "  WHERE A.TRAN_DAY = B.TRAN_DAY";
            query += "    AND A.ITEM = B.ITEM";
            query += "    AND A.TRAN_DAY = '" + today + "'";
            //query += "    AND A.END > A.AVG_5";
            query += "    AND B.FORE > 0";
            query += "    AND B.INS > 0";
            query += "    AND A.END BETWEEN A.BOL_BTM * 0.7 AND A.BOL_BTM * 0.999";
            query += "    AND ( (A.END BETWEEN A.AVG_5 * 0.975 AND A.AVG_5 * 1.025 ) OR (A.END BETWEEN A.AVG_10 * 0.975 AND A.AVG_10 * 1.025 ) )";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.전략, "[볼린져밴드 하단 근접(혹은 돌파) 및 쌍매수 종목 저장 완료]");
        }


        // 종합주가지수 저장
        public void RetMarketIndex(string today)
        {
            _formObj._api.ApiT1514("종합주가지수 조회", today);
        }


        // 종합주가지수 저장(해외)
        public void RetMarketIndexAbroad(string today)
        {
            _formObj._api.ApiT3518("종합주가지수 조회(해외)", today);
        }


        // 종합주가지수 투자자별 거래금액 저장
        public void RetMarketIndexInvestor(string fromDate, string toDate)
        {
            _formObj._api.ApiT1665("종합주가지수 투자자별 거래금액 조회", fromDate, toDate);
        }
    }
}