using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;


namespace SMWJ
{
    public class Sell
    {
        private Form1 _formObj;     // form object        

        public Timer _timer;

        private Dictionary<string, Dictionary<string, string>> _sellItems = new Dictionary<string, Dictionary<string, string>>(); // 매도 대상 종목 메모리에 저장
        private Dictionary<string, string> _boughtItems = new Dictionary<string, string>(); // DB조회한 매도 대상 종목 메모리에 저장

        public Sell(Form1 formObj)
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


        public void SellStarter()
        {
            // 타이머 생성 및 시작
            _timer = new System.Timers.Timer();
            _timer.Interval = 5 * 1000; // 5초
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _timer.Start();
        }


        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            int hhminss = int.Parse(DateTime.Now.ToString("HHmmss"));
            string today = DateTime.Now.ToString("yyyyMMdd");

            _formObj.ShowThreadState(hhminss.ToString(), "S");

            // 매도 판단
            if (hhminss >= 90000 && hhminss <= 153000)
            {
                RetDbAcctItem(today);                
            }
            // 프로그램 종료
            else if (hhminss >= 160000)
            {
                _formObj.ReqAppStop();
            }
        }


        // 계좌 DB잔고 종목 조회
        public void RetDbAcctItem(string today)
        {
            _formObj.Logger(Log.매도, "[계좌 DB잔고 종목 조회 시작]");

            string query = "";
            query += " SELECT A.MODE";
            query += "      , A.ITEM";
            query += "      , SUBSTR(A.TRAN_ID,1,8)  AS BDAY";
            query += "      , IFNULL(B.TRAN_DAY,'-') AS NDAY";
            query += "      , D.SEQ - C.SEQ          AS SEQ_DIFF";
            query += "   FROM TRAN A";
            query += "   LEFT";
            query += "  OUTER";
            query += "   JOIN TRAN_DAY_CAL B";
            query += "     ON SUBSTR(A.TRAN_ID,1,8) = B.POOL_DAY";
            query += "   JOIN TRAN_DAY_CAL C";
            query += "     ON SUBSTR(A.TRAN_ID,1,8) = C.TRAN_DAY";
            query += "   JOIN TRAN_DAY_CAL D";
            query += "     ON A.TRAN_DAY = D.TRAN_DAY";
            query += "  WHERE A.TRAN_DAY = '" + today + "'";
            query += "    AND A.TRAN_SP  = 2";
            query += "    AND A.SELL_ORDER_NO IS NULL";
            query += "  ORDER BY A.MODE";

            _boughtItems.Clear();
            _boughtItems = new Dictionary<string,string>();

            ArrayList boughtList = _formObj._db.SelectQuery(query);

            foreach(Dictionary<string,string> tmp in boughtList) 
            {
                //_boughtItems.Add(tmp["ITEM"], tmp["MODE"] + ";" + tmp["BDAY"] + ";" + tmp["NDAY"]);
                string temp = tmp["MODE"] + ";" + tmp["BDAY"] + ";" + tmp["NDAY"] + ";" + tmp["SEQ_DIFF"];
                _boughtItems[tmp["ITEM"]] = temp;
            }

            RetAcctItem();
        }


        // 계좌 잔고 종목 조회
        public void RetAcctItem()
        {
            _formObj.Logger(Log.매도, "[계좌 잔고 종목 조회 시작]");

            Dictionary<string, string> param = new Dictionary<string, string>();

            param.Add("계좌번호", Constants.ACCT);

            _formObj.OpenApiRequest("계좌수익률요청", "OPT10085", 0, Constants.RET_ACCT_ITEM.ToString(), param);
        }


        // 계좌 잔고 종목 조회 콜백
        public void RetAcctItem_Callback(ArrayList acctItems, string today)
        {
            double totProfit = 0;
            int accntCnt = 0;

            for (int i = 0; i < acctItems.Count; i++) 
            {
                Dictionary<string, string> row = (Dictionary<string, string>)acctItems[i];

                string item = row["item"];
                string tranDay = row["tranDay"];
                double amt = double.Parse(row["amt"]);
                double price = Math.Abs(double.Parse(row["price"]));
                double buyPrice = double.Parse(row["buyPrice"]);
                double buyTot = double.Parse(row["buyTot"]);

                if (buyPrice == 0)
                {
                    continue;
                }

                // 2. 두 번째 콜백 이후일 때
                if (_sellItems.ContainsKey(item))
                {
                    Dictionary<string, string> savedRow = _sellItems[item];

                    // 4. 세 번째 콜백 이후일 때
                    if (savedRow.ContainsKey("price-1"))
                    {
                        // 6. 네 번째 콜백 이후일 때
                        if (savedRow.ContainsKey("price-2"))
                        {
                            savedRow["price-3"] = savedRow["price-2"];
                            savedRow["price-2"] = savedRow["price-1"];
                            savedRow["price-1"] = savedRow["price"];
                            savedRow["price"] = row["price"];

                            _sellItems.Remove(item);
                            _sellItems[item] = savedRow;
                        }
                        // 5. 세 번째 콜백일 때
                        else
                        {
                            savedRow["price-2"] = savedRow["price-1"];
                            savedRow["price-1"] = savedRow["price"];
                            savedRow["price"] = row["price"];

                            _sellItems.Remove(item);
                            _sellItems[item] = savedRow;
                        }
                    }
                    // 3. 두 번째 콜백일 때
                    else
                    {
                        savedRow["price-1"] = savedRow["price"];
                        savedRow["price"] = row["price"];

                        _sellItems.Remove(item);
                        _sellItems[item] = savedRow;
                    }
                }
                // 1. 최초 콜백을 받았을 때
                else {
                    _sellItems.Add(item, row);
                }
                
                double profitRate = (price / buyPrice);
                double profit = (price * amt) - buyTot;

                totProfit = totProfit + profit;

                accntCnt++;

                _formObj.Logger(Log.매도, "종목코드 : " + item + ", 수량 : " + amt + ", 현재가 : " + price + ", 매입가 : " + buyPrice + ", 수익률 : " + Math.Round(profitRate, 2).ToString() + ", 수익금액 : " + profit);

                // 해당 종목의 매도 여부 판단
                ReqSellDetermine( _sellItems[item], today );
            }

            _formObj.Logger(Log.매도, "건수 : " + accntCnt + ", 전체 수익 금액 : " + totProfit);
            _formObj.Logger(Log.매도, "[계좌 잔고 종목 조회 끝]");
        }


        // 매도 판단
        private void ReqSellDetermine(Dictionary<string, string> row, string today)
        {

            int hhmin = int.Parse(DateTime.Now.ToString("HHmm"));
            
            string item = row["item"];
            string tranDay = row["tranDay"];
            double price = Math.Abs(Convert.ToDouble(row["price"]));
            double buyPrice = Math.Abs(Convert.ToDouble(row["buyPrice"]));
            double buyCnt = Math.Abs(Convert.ToDouble(row["amt"]));
            double profitRate = price / buyPrice;

            string[] tmp = ((_boughtItems[item]).Trim()).Split(new Char[] { ';' });

            int mode = int.Parse(tmp[0]);
            string bday = tmp[1];
            string nday = tmp[2];
            int seqDiff = int.Parse(tmp[3]);

            _formObj.Logger(Log.매도, "[매도 판단 종목] 종목코드 : " + item + ", 전략 : " + mode + ", 매수일자 : " + bday + ", 다음거래일 : " + nday + ", 매수 후 " + seqDiff + " 일 경과");

            double price_1 = 0;
            double price_2 = 0;
            double price_3 = 0;

            if (row.ContainsKey("price-1"))
            {
                price_1 = Math.Abs(Convert.ToDouble(row["price-1"]));
            }

            if (row.ContainsKey("price-2"))
            {
                price_2 = Math.Abs(Convert.ToDouble(row["price-2"]));
            }

            if (row.ContainsKey("price-3"))
            {
                price_3 = Math.Abs(Convert.ToDouble(row["price-3"]));
            }

            double diff = price - price_3;
            double diff_1 = price - price_1;
            double diff_2 = price_1 - price_2;
            double diff_3 = price_2 - price_3;


            if (mode == 7)
            {
                if (seqDiff == 5 && hhmin >= 1522)
                {
                    ReqSell(row);

                    _formObj.Logger(Log.매도, "[5일 후 매도 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                }
                else if (seqDiff > 5)
                {
                    if (profitRate >= 1.01)
                    {
                        // 내림세일 때
                        // 1. 세 번 연속 하락
                        // 2. 두 번 연속 하락 && 마지막 하락 폭이 더 큼
                        // 3. 불연속 하락 && 불연속 구간 전체의 하락
                        if ((diff_1 < 0 && diff_2 < 0 && diff_3 < 0) || ((diff_1 < 0 && diff_2 < 0) && (diff_1 < diff_2)) || ((diff_1 < 0 && diff_3 < 0) && (diff < 0)))
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[익절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                        // 가격 변동 없다가 한 번 내려갔을 때
                        else if (diff_1 < 0 && diff_2 == 0 && diff_3 == 0)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[가격 변폭 적은 종목 익절 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                    }
                }
            }
            else
            {
                // 1% 이상일 경우 시장가 매도(익절)
                //  > 5분내 익절
                //  > 장중 거래량 감소시 익절
                //  > 일반 익절
                // -20% 이하일 경우 시장가 매도(손절)
                // 익절도 손절도 하지 못할 경우, 당일 종가 매도            
                if (price_1 > 0 && price_2 > 0 && price_3 > 0)
                {
                    // 9:00~9:05 사이에는 두 번 연속 하락시 매도
                    if (hhmin <= 905)
                    {
                        if (diff_1 < 0 && diff_2 < 0 && profitRate >= 1.01)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[5분내 익절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                        // -20 이하일 경우 손절
                        /*
                        else if (profitRate <= 0.8)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[손절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                        */

                        if (mode == 99 && profitRate >= 1.005)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[추매 종목 청산 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                    }
                    // 기본적으로, 장마감 임박해서는 -3% ~ 1% 일 경우 매도
                    // 당일 매수한 상한가 전략은 제외(익일 매도함, 수익률 -9% 이상)
                    // 당일 매수한 쌍매수(시가) 전략은 제외(익일 매도함, 수익률 -5% 이상)
                    // 당일 매수한 쌍매수(종가) 전략은 제외(익일 매도함)
                    // 매수 당일에는, 등락률이 -3% 아래일 경우 손절 불가
                    // 매수 후 1일 이상 경과할 경우, 손절폭 확대(-9% ~ 1%)
                    else if (hhmin >= 1522)
                    {
                        //_formObj.Logger(Log.매도, "[장마감 임박] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 전략 : " + mode + ", 매수일자 : " + bday + ", 현재일 : " + today);

                        if (mode == 1)
                        {
                            if (today == nday)
                            {
                                // 상한가 종목은 변동성이 매우 큼 > 단시간에 회복할 가능성이 낮음 > 빨리 팔아야함
                                if (profitRate >= 0.91 && profitRate < 1.01)
                                {
                                    ReqSell(row);

                                    _formObj.Logger(Log.매도, "[장마감 임박, 상한가 D+1 청산 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                                }
                            }
                        }
                        else if (mode == 2)
                        {
                            if (today == nday)
                            {
                                // 이것보다 더 손실나는 종목까지 손절하면 닶없음...
                                if (profitRate >= 0.95 && profitRate < 1.01)
                                {
                                    ReqSell(row);

                                    _formObj.Logger(Log.매도, "[장마감 임박, 쌍매수(시가) D+1 청산 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                                }
                            }
                        }
                        else if (mode == 4)
                        {
                            if (today == nday)
                            {
                                // 쌍매수(종가) 종목은 변동성이 크지 않음 > 단시간에 회복할 가능성이 있음 > 기다릴만 함
                                if (profitRate >= 1 && profitRate < 1.01)
                                {
                                    ReqSell(row);

                                    _formObj.Logger(Log.매도, "[장마감 임박, 쌍매수(종가) 청산 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                                }
                            }
                        }
                        else if (mode == 6)
                        {
                            // 손절 없음
                        }
                        else if (mode == 99)
                        {
                            // 손절 없음
                        }
                        else
                        {
                            if (today == bday)
                            {
                                if (profitRate >= 0.97 && profitRate < 1.01)
                                {
                                    ReqSell(row);

                                    _formObj.Logger(Log.매도, "[장마감 임박, 당일 청산 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                                }
                            }
                            else
                            {
                                if (profitRate >= 0.91 && profitRate < 1.01)
                                {
                                    ReqSell(row);

                                    _formObj.Logger(Log.매도, "[장마감 임박, D+2 이후 청산 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                                }
                            }
                        }
                    }
                    else
                    {
                        if (profitRate >= 1.01)
                        {
                            // 내림세일 때
                            // 1. 세 번 연속 하락
                            // 2. 두 번 연속 하락 && 마지막 하락 폭이 더 큼
                            // 3. 불연속 하락 && 불연속 구간 전체의 하락
                            if ((diff_1 < 0 && diff_2 < 0 && diff_3 < 0) || ((diff_1 < 0 && diff_2 < 0) && (diff_1 < diff_2)) || ((diff_1 < 0 && diff_3 < 0) && (diff < 0)))
                            {
                                ReqSell(row);

                                _formObj.Logger(Log.매도, "[익절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                            }
                            // 가격 변동 없다가 한 번 내려갔을 때
                            else if (diff_1 < 0 && diff_2 == 0 && diff_3 == 0)
                            {
                                ReqSell(row);

                                _formObj.Logger(Log.매도, "[가격 변폭 적은 종목 익절 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                            }
                        }

                        if (mode == 99 && profitRate >= 1.005)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[추매 종목 청산 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }

                        // -20 이하일 경우 손절
                        /*
                        else if (profitRate <= 0.8)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[손절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                        */
                    }
                }
                /*
                else
                {
                    // 장시작 5분 이내에는 1% 이상이고, 한 번이라도 하락하면 매도
                    if (price_1 > 0 && hhmin <= 905)
                    {
                        if (diff_1 < 0 && profitRate >= 1.01)
                        {
                            ReqSell(row);

                            _formObj.Logger(Log.매도, "[5분내 익절 종목 발생] 종목코드 : " + item + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + ((price - buyPrice) * buyCnt).ToString());
                        }
                    }
                }
                */
            }
        }


        // 매도 요청
        public void ReqSell(Dictionary<string, string> row)
        {
            // 과부하 막기 위한 슬립
            Delay(Constants.SLEEP_TIME);

            // 거래구분 취득
            // 00:지정가, 03:시장가, 05:조건부지정가, 06:최유리지정가, 07:최우선지정가,
            // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
            // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
            string orderCond = "03";

            // 매매구분 취득
            // (1:신규매수, 2:신규매도 3:매수취소, 
            // 4:매도취소, 5:매수정정, 6:매도정정)
            int trType = 2;

            string acctNo = Constants.ACCT;
            string item = row["item"];
            string tranDay = row["tranDay"];
            int amt = int.Parse(row["amt"]);
            int price = int.Parse(row["price"]);

            if ("03".Equals(orderCond))
            {
                price = 0;
            }

            _formObj.Logger(Log.매도, "[매도 요청] 종목코드 : {0}, 수량 : {1}", item, amt);

            // 주식주문
            _formObj.OpenApiOrder(
                  Constants.RET_SELL_DET_INFO // 화면번호
                , acctNo                      // 계좌번호
                , trType                      // 매매구분
                , item                        // 종목코드
                , amt                         // 주문수량
                , price                       // 주문가격
                , orderCond                   // 거래구분
                , ""                          // 원주문번호
                );

            string query = "";
            query += " UPDATE TRAN";
            query += "    SET TRAN_SP = 3";
            query += "      , TIME_3  = NOW()";
            query += "  WHERE TRAN_SP        = 2";
            query += "    AND TRAN_DAY       = '" + tranDay + "'";
            query += "    AND ITEM           = '" + item + "'";
            query += "    AND SELL_ORDER_NO IS NULL";

            _formObj._db.InsertQuery(query);
        }


        // 매도 요청 콜백
        public void ReqSell_Callback(string orderNo, double avgPrice, int orderAmt, int remainAmt, int totalPrice, int todayFee, int todayTax, string today, string item)
        {
            string query = "";
            query += " UPDATE TRAN";
            query += "    SET SELL_ORDER_NO = '" + orderNo + "'";
            query += "      , SELL_PRICE    = " + avgPrice;
            query += "      , SELL_CNT      = " + (remainAmt == 0 ? orderAmt : (orderAmt - remainAmt));
            query += "      , SELL_TOT      = " + totalPrice;
            query += "      , SELL_FEE      = " + todayFee;
            query += "      , SELL_TAX      = " + todayTax;
            query += "      , SELL_AMT      = " + (remainAmt == 0 ? (totalPrice - todayFee - todayTax) : 0);
            query += "      , INCOME        = " + (remainAmt == 0 ? (totalPrice - todayFee - todayTax) : 0) + " - BUY_TOT - BUY_FEE";
            query += "      , FINAL_RATE    = ROUND((" + (remainAmt == 0 ? (totalPrice - todayFee - todayTax) : 0) + " - BUY_TOT - BUY_FEE) / BUY_TOT * 100, 2)";
            query += "      , TRAN_SP       = '" + (remainAmt == 0 ? "4" : "3") + "'";
            query += "      , TIME_4        = " + (remainAmt == 0 ? "NOW()" : "NULL");
            query += "  WHERE TRAN_DAY       = '" + today + "'";
            query += "    AND ITEM           = '" + item + "'";
            query += "    AND TRAN_SP        = 3";

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.매도, "[매도 요청 콜백] 종목코드 : {0}, 매도주문수량 : {1}, 매도가격 : {2}, 업데이트 건수 : {3}", item, orderAmt, avgPrice);
        }


        // 전량매도
        public void RetAcctItemSale()
        {
            _formObj.Logger(Log.매도, "[전량 매도 위한 계좌 잔고 종목 조회 시작]");

            Dictionary<string, string> param = new Dictionary<string, string>();

            param.Add("계좌번호", Constants.ACCT);

            _formObj.OpenApiRequest("계좌수익률요청", "OPT10085", 0, Constants.RET_ACCT_ITEM_SALE, param);
        }


        // 전량매도 콜백
        public void RetAcctItemSale_Callback(ArrayList acctItems)
        {
            double totProfit = 0;
            int totCnt = 0;
            string today = DateTime.Now.ToString("yyyyMMdd");

            foreach (Dictionary<string, string> item in acctItems)
            {
                string itemCd = item["item"];
                double amt = Convert.ToInt32(item["amt"]);
                double price = Math.Abs(Convert.ToInt32(item["price"]));
                double buyPrice = Convert.ToInt32(item["buyPrice"]);
                double buyTot = Convert.ToInt32(item["buyTot"]);
                    
                if (amt > 0)
                {
                    double profitRate = (price / buyPrice);
                    double profit = (price * amt) - buyTot;
                    totProfit += profit;

                    totCnt++;

                    item["price"] = "0";

                    ReqSell(item);

                    _formObj.Logger(Log.매도, "종목코드 : " + item["item"] + ", 현재가 : " + price + ", 매입가 : " + buyPrice + ", 수익률 : " + profitRate.ToString() + ", 수익금액 : " + profit.ToString());
                }
            }

            _formObj.Logger(Log.매도, "건수 : " + totCnt + ", 전체 수익 금액 : " + totProfit.ToString());
            _formObj.Logger(Log.매도, "[전량 매도 위한 계좌 잔고 종목 조회 후 매도요청]");
        }
    }
}
