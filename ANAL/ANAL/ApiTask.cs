using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XA_DATASETLib;
using XA_SESSIONLib;

namespace SMWJ
{
    public class ApiTask
    {
        //api 전역변수 선언
        XASession _xaSession = new XASession();

        XAQuery t8407 = new XAQuery();
        XAQuery t8413 = new XAQuery();
        XAQuery t1717 = new XAQuery();
        XAQuery t8407hoga = new XAQuery();
        XAQuery t8430 = new XAQuery();
        XAQuery t1404 = new XAQuery();
        XAQuery t3320 = new XAQuery();
        XAQuery t1514 = new XAQuery();
        XAQuery t3518 = new XAQuery();
        XAQuery t1665 = new XAQuery();

        XAReal xaReal = new XAReal();

        // form object
        private Form1 _formObj;

        // 해외지수코드
        private string _marketIndexAbr = "";

        public ApiTask(Form1 formObj)
        {            
            // 이벤트 등록
            ((XA_SESSIONLib._IXASessionEvents_Event)_xaSession).Login += new _IXASessionEvents_LoginEventHandler(ApiLoginR);

            t8407.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT8407R);
            t8413.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT8413R);
            t1717.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT1717R);
            t8407hoga.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT8407Rhoga);
            t8430.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT8430R);
            t1404.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT1404R);
            t3320.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT3320R);
            t1514.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT1514R);
            t3518.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT3518R);
            t1665.ReceiveData += new _IXAQueryEvents_ReceiveDataEventHandler(ApiT1665R);

            xaReal.ReceiveRealData += new _IXARealEvents_ReceiveRealDataEventHandler(ApiRealR);
            //query.ReceiveMessage += new _IXAQueryEvents_ReceiveMessageEventHandler(OpenApiReceiveMsg);
            //query.ReceiveMessage += new _IXAQueryEvents_ReceiveMessageEventHandler(OpenApiReceiveMsg);

            this._formObj = formObj;
        }


        // 조회수 제한 회피를 위한 프로그램지연
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


        // 로그인
        public bool ApiLogin()
        {
            bool rslt = false;

            if (_xaSession.ConnectServer(Constants.CONN_ADDR, Constants.CONN_PORT) == true)
            {
                if (((XA_SESSIONLib.IXASession)_xaSession).Login(Constants.ID, Constants.PW, Constants.CP, 0, false) == true)
                {
                    rslt = true;
                }
                else
                {
                    rslt = false;
                }
            }
            else
            {
                rslt = false;
            }

            return rslt;
        }


        // 로그인 이벤트
        private void ApiLoginEvent(string code, string msg)
        {
            //Logger(Log.일반, "[로그인 결과] : " + code + ", " + msg);
        }


        // 로그아웃
        public void ApiLogout()
        {
            ((XA_SESSIONLib.IXASession)_xaSession).Logout();
            _xaSession.DisconnectServer();
        }


        // 접속 상태 확인
        public bool ApiGetConnStatus()
        {
            bool rslt = false;
            if (_xaSession.IsConnected() == false)
            {
                rslt = false;
            }
            else
            {
                rslt = true;
            }

            return rslt;
        }


        // 로그인 이벤트 핸들러
        public void ApiLoginR(string szCode, string szMsg)
        {
            if (szCode == "0000")
            {
                ApiRealMarketStatus();

                _formObj._sttg = new Strategy(_formObj);

                _formObj.ReqThreadStart();
            }

            _formObj.Logger(Log.일반, "[로그인 결과 : {0}, {1}]", szCode, szMsg);
        }


        // API실시간조회 장운영정보
        public void ApiRealMarketStatus()
        {
            xaReal.ResFileName = "C:\\eBest\\xingAPI\\Res\\JIF.res";
            xaReal.SetFieldData("InBlock", "jangubun", "0");

            xaReal.AdviseRealData();
        }        


        // API실시간조회 데이터수신 이벤트
        public void ApiRealR(string szTrCode)
        {
            if (szTrCode == "JIF")
            {
                string market = xaReal.GetFieldData("OutBlock", "jangubun");
                string status = xaReal.GetFieldData("OutBlock", "jstatus");

                _formObj.Logger(Log.일반, "[장운영정보 요청 : {0}, {1}]", market, status);

                if (status == "24" || status == "25") // 24: 장개시 5분전, 25: 장개시 10분전
                {
                    _formObj._bizDayYn = true;

                    xaReal.UnadviseRealData();
                }                
            }
        }


        // API조회 공통함수
        public void ApiRequest(XAQuery query, bool msgYn, string reqName)
        {
            // 과부하 막기 위한 슬립
            Delay(Constants.SLEEP_TIME);

            int nRet = query.Request(msgYn);

            if (Error.IsError(nRet))
            {
                _formObj.ShowTrMsg("요청 성공");
                _formObj.Logger(Log.일반, "[TR 요청 {0}] : " + Error.GetErrorMessage(), reqName);
            }
            else
            {
                _formObj.ShowTrMsg("요청 실패");
                _formObj.Logger(Log.에러, "[TR 요청 {0}] : " + Error.GetErrorMessage(), reqName);
            }
        }

        
        // 종합주가지수 투자자별 거래금액 조회
        public void ApiT1665(string reqName, string fromDate, string toDate)
        {
            t1665.ResFileName = "C:\\eBest\\xingAPI\\Res\\t1665.res";
            t1665.SetFieldData("t1665InBlock", "market", 0, "1"); // 1: kospi, 2: kospi200, 3: kosdaq
            t1665.SetFieldData("t1665InBlock", "upcode", 0, "001"); // 001: kospi, 301: kosdaq
            t1665.SetFieldData("t1665InBlock", "gubun2", 0, "1"); // 1: 수치, 2: 누적
            t1665.SetFieldData("t1665InBlock", "gubun3", 0, "1"); // 1: 일, 2: 주, 3: 월
            t1665.SetFieldData("t1665InBlock", "from_date", 0, fromDate);
            t1665.SetFieldData("t1665InBlock", "to_date", 0, toDate);

            ApiRequest(t1665, false, reqName);
        }
        public void ApiT1665R(string trCode)
        {
            string blockNm = "t1665OutBlock1";
            int nCnt = t1665.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string ant = t1665.GetFieldData(blockNm, "sa_08", i); // 개인
                string fore = t1665.GetFieldData(blockNm, "sa_17", i); // 외인계금액(등록+미등록)
                string ins = t1665.GetFieldData(blockNm, "sa_18", i);
                string tranDay = t1665.GetFieldData(blockNm, "date", i);

                string query = "";
                query += " INSERT INTO INVESTOR ";
                query += "      ( ITEM";
                query += "      , TRAN_DAY";
                query += "      , ANT";
                query += "      , FORE";
                query += "      , INS";
                query += "      )";
                query += " VALUES ";
                query += "      ( '001'";
                query += "      , '" + tranDay + "'";
                query += "      , " + ant;
                query += "      , " + fore;
                query += "      , " + ins;
                query += "      ) ";
                query += "     ON DUPLICATE KEY ";
                query += " UPDATE ANT  = " + ant;
                query += "      , FORE = " + fore;
                query += "      , INS  = " + ins;
                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "일자 : {0} | 외인계 :{1} | 기관계 :{2}", tranDay, fore, ins);
            }
        }


        // 종합주가지수 조회
        public void ApiT1514(string reqName, string today)
        {
            string[] items = { "001", "301" }; // 001: 코스피, 301: 코스닥

            foreach(string item in items) 
            {
                t1514.ResFileName = "C:\\eBest\\xingAPI\\Res\\t1514.res";
                t1514.SetFieldData("t1514InBlock", "upcode", 0, item);
                t1514.SetFieldData("t1514InBlock", "gubun2", 0, "1"); // 1: 일, 2: 주, 3: 월
                t1514.SetFieldData("t1514InBlock", "cts_date", 0, today);
                t1514.SetFieldData("t1514InBlock", "cnt", 0, "16"); // 조회건수
                t1514.SetFieldData("t1514InBlock", "rate_gbn", 0, "1"); // 1: 거래량비중, 2: 거래대금비중

                ApiRequest(t1514, false, reqName);
            }
        }
        public void ApiT1514R(string trCode)
        {
            string blockNm = "t1514OutBlock1";            
            int nCnt = t1514.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string item = t1514.GetFieldData(blockNm, "upcode", i);
                string end = t1514.GetFieldData(blockNm, "jisu", i);
                string low = t1514.GetFieldData(blockNm, "lowjisu", i);
                string high = t1514.GetFieldData(blockNm, "highjisu", i);
                string begin = t1514.GetFieldData(blockNm, "openjisu", i);
                string tranDay = t1514.GetFieldData(blockNm, "date", i);

                string query = "";
                query += " INSERT INTO MARKET_INDEX ";
                query +=     "      ( ITEM";
                    query += "      , TRAN_DAY";
                    query += "      , BEGIN";
                    query += "      , LOW";
                    query += "      , HIGH";
                    query += "      , END";
                    query += "      , WORK_MAN";
                    query += "      , WORK_TIME";
                    query += "      )";
                    query += " VALUES ";
                    query += "      ( '" + item + "'";
                    query += "      , '" + tranDay + "'";
                    query += "      , " + begin;
                    query += "      , " + low;
                    query += "      , " + high;
                    query += "      , " + end;
                    query += "      , 'P'";
                    query += "      , NOW()";
                    query += "      ) ";
                    query += "     ON DUPLICATE KEY ";
                    query += " UPDATE BEGIN     = " + begin;
                    query += "      , LOW       = " + low;
                    query += "      , HIGH      = " + high;
                    query += "      , END       = " + end;
                    query += "      , WORK_MAN  = 'P'";
                    query += "      , WORK_TIME = NOW()";

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "일자 : {2} | 지수코드 :{0} | 지수 종가 :{1}", item, end, tranDay);
            }
        }


        // 주요 해외지수 조회
        public void ApiT3518(string reqName, string today)
        {
            string[] items = { "R-USDKRWSMBS", "S-DJI@DJI", "S-NAS@IXIC", "S-SPI@SPX", "S-NII@NI225" }; // 원달러, 다우존스산업

            foreach (string item in items)
            {
                string[] temp = item.Split(new char[]{'-'});

                t3518.ResFileName = "C:\\eBest\\xingAPI\\Res\\t3518.res";
                t3518.SetFieldData("t3518InBlock", "kind", 0, temp[0]);
                t3518.SetFieldData("t3518InBlock", "symbol", 0, temp[1]);
                t3518.SetFieldData("t3518InBlock", "cnt", 0, "1"); // 입력건수
                t3518.SetFieldData("t3518InBlock", "jgbn", 0, "1"); // 0: 일, 1: 주, 2: 월, 3: 분
                t3518.SetFieldData("t3518InBlock", "cts_date", 0, today);

                ApiRequest(t3518, false, reqName);

                _marketIndexAbr = temp[1];
            }
        }
        public void ApiT3518R(string trCode)
        {
            string itemCd = _marketIndexAbr;
            string blockNm = "t3518OutBlock1";
            int nCnt = t3518.GetBlockCount(blockNm);            

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {                
                string item = t3518.GetFieldData(blockNm, "symbol", i);
                string end = t3518.GetFieldData(blockNm, "price", i);
                string low = t3518.GetFieldData(blockNm, "low", i);
                string high = t3518.GetFieldData(blockNm, "high", i);
                string begin = t3518.GetFieldData(blockNm, "open", i);
                string tranDay = t3518.GetFieldData(blockNm, "date", i);

                string query = "";
                if (itemCd == "USDKRWSMBS")
                {
                    query += " INSERT INTO MARKET_INDEX ";
                    query += "      ( ITEM";
                    query += "      , TRAN_DAY";
                    query += "      , BEGIN";
                    query += "      , LOW";
                    query += "      , HIGH";
                    query += "      , END";
                    query += "      , WORK_MAN";
                    query += "      , WORK_TIME";
                    query += "      )";
                    query += " VALUES ";
                    query += "      ( '" + itemCd + "'";
                    query += "      , '" + tranDay + "'";
                    query += "      , " + begin;
                    query += "      , " + low;
                    query += "      , " + high;
                    query += "      , " + end;
                    query += "      , 'P'";
                    query += "      , NOW()";
                    query += "      ) ";
                    query += "     ON DUPLICATE KEY ";
                    query += " UPDATE BEGIN     = " + begin;
                    query += "      , LOW       = " + low;
                    query += "      , HIGH      = " + high;
                    query += "      , END       = " + end;
                    query += "      , WORK_MAN  = 'P'";
                    query += "      , WORK_TIME = NOW()";
                }
                else
                {
                    query += " INSERT INTO MARKET_INDEX ";
                    query += "      ( ITEM";
                    query += "      , TRAN_DAY";
                    query += "      , BEGIN";
                    query += "      , LOW";
                    query += "      , HIGH";
                    query += "      , END";
                    query += "      , WORK_MAN";
                    query += "      , WORK_TIME";
                    query += "      )";
                    query += " VALUES ";
                    query += "      ( '" + itemCd + "'";
                    query += "      , '" + tranDay + "'";
                    query += "      , " + begin + " * 100";
                    query += "      , " + low + " * 100";
                    query += "      , " + high + " * 100";
                    query += "      , " + end + " * 100";
                    query += "      , 'P'";
                    query += "      , NOW()";
                    query += "      ) ";
                    query += "     ON DUPLICATE KEY ";
                    query += " UPDATE BEGIN     = " + begin + " * 100";
                    query += "      , LOW       = " + low + " * 100";
                    query += "      , HIGH      = " + high + " * 100";
                    query += "      , END       = " + end + " * 100";
                    query += "      , WORK_MAN  = 'P'";
                    query += "      , WORK_TIME = NOW()";
                }

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "일자 : {2} | 지수코드 :{0} | 지수 종가 :{1}", itemCd, end, tranDay);
            }
        }



        // 주식마스터조회
        public void ApiT8430(string reqName)
        {
            t8430.ResFileName = "C:\\eBest\\xingAPI\\Res\\t8430.res";
            t8430.SetFieldData("t8430InBlock", "gubun", 0, "0"); // 0: 전체, 1: 코스피, 2: 코스닥

            ApiRequest(t8430, false, reqName);
        }
        public void ApiT8430R(string trCode)
        {
            string blockNm = "t8430OutBlock";
            int nCnt = t8430.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string item = t8430.GetFieldData(blockNm, "shcode", i);
                string itemNm = t8430.GetFieldData(blockNm, "hname", i);
                string itemSp = ((t8430.GetFieldData(blockNm, "gubun", i)) == "1")?"P":"D";

                string query = "";
                query += " INSERT INTO ITEM";
                query += " (ITEM, ITEM_NM, ITEM_SP, USE_YN, WORK_MAN, WORK_TIME)";
                query += " VALUES";
                query += " ('" + item + "', '" + itemNm + "', '" + itemSp + "', 'Y', 'P', NOW())";
                query += " ON DUPLICATE KEY UPDATE ITEM_NM   = '" + itemNm + "'";
                query += "                       , ITEM_SP   = '" + itemSp + "'";
                query += "                       , USE_YN    = 'Y'";
                query += "                       , WORK_TIME = NOW()";

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "종목코드 :{0} | 지수종가 :{1}", item, itemNm, itemSp);
            }

            ApiT1404("관리종목 조회");
        }


        // 관리종목 조회
        public void ApiT1404(string reqName)
        {
            t1404.ResFileName = "C:\\eBest\\xingAPI\\Res\\t1404.res";
            t1404.SetFieldData("t1404InBlock", "gubun", 0, "0"); // 0: 전체, 1: 코스피, 2: 코스닥
            t1404.SetFieldData("t1404InBlock", "jongchk", 0, "1"); // 1: 관리종목, 2: 불성실, 3: 투자유의

            ApiRequest(t1404, false, reqName);
        }
        public void ApiT1404R(string trCode)
        {
            string blockNm = "t1404OutBlock1";
            int nCnt = t1404.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string item = t1404.GetFieldData(blockNm, "shcode", i);
                string state = "관리종목";

                string query = "";
                query += " INSERT INTO PRICE_SMMRY";
                query += " (TRAN_DAY, ITEM, STATE, WORK_MAN, WORK_TIME)";
                query += " VALUES";
                query += " (DATE_FORMAT(NOW(), '%Y%m%d')";
                query += " ,'" + item + "'";
                query += " ,'" + state + "'";
                query += " , 'P'";
                query += " , NOW()";
                query += " )";
                query += " ON DUPLICATE KEY UPDATE STATE            = '" + state + "'";
                query += "                       , WORK_MAN         = 'P'";
                query += "                       , WORK_TIME        = NOW()";

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "종목코드 :{0}", item);
            }

            _formObj._sttg.RetRefreshItemMst_Callback();
        }


        // 시가총액 및 재무정보 조회
        public void ApiT3320(string reqName, string item)
        {
            t3320.ResFileName = "C:\\eBest\\xingAPI\\Res\\t3320.res";
            t3320.SetFieldData("t3320InBlock", "gicode", 0, item);

            ApiRequest(t3320, false, reqName);
        }
        public void ApiT3320R(string trCode)
        {
            string blockNm0 = "t3320OutBlock";
            string blockNm1 = "t3320OutBlock1";
            int nCnt0 = t3320.GetBlockCount(blockNm0);
            int nCnt1 = t3320.GetBlockCount(blockNm1);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt0; i++)
            {
                string item = (t3320.GetFieldData(blockNm1, "gicode", i)).Replace("A","");
                string itemNm = t3320.GetFieldData(blockNm0, "company", i);
                double stckAmt = double.Parse(t3320.GetFieldData(blockNm0, "gstock", i));
                double marketCap = double.Parse(t3320.GetFieldData(blockNm0, "sigavalue", i));
                int endPrice = Int32.Parse(t3320.GetFieldData(blockNm0, "price", i));
                int ydayPrc = Int32.Parse(t3320.GetFieldData(blockNm0, "jnilclose", i));
                int diff = endPrice - ydayPrc;
                double rate = 0;
                if (ydayPrc > 0)
                {
                    rate = Math.Round((double)((diff / ydayPrc) * 100), 2);
                }                
                double per = double.Parse(t3320.GetFieldData(blockNm1, "per", i));
                double eps = double.Parse(t3320.GetFieldData(blockNm1, "eps", i));
                double pbr = double.Parse(t3320.GetFieldData(blockNm1, "pbr", i));
                double roe = double.Parse(t3320.GetFieldData(blockNm1, "roe", i));
                double ebit = double.Parse(t3320.GetFieldData(blockNm1, "ebitda", i));

                string query = "";
                query += " INSERT INTO PRICE_SMMRY";
                query += " (TRAN_DAY, ITEM, ITEM_NM, PRICE, PRC_DIFF, RATE, MARKET_CAP, SHARE_NUM, PER, EPS, ROE, PBR, EBIT, WORK_MAN, WORK_TIME)";
                query += " VALUES";
                query += " (DATE_FORMAT(NOW(), '%Y%m%d')";
                query += " ,'" + item + "'";
                query += " ,SUBSTR('" + itemNm + "', 1, 50)";
                query += " ,NULLIF('" + endPrice + "','')";
                query += " ,NULLIF('" + diff + "','')";
                query += " ,NULLIF('" + rate + "','')";
                query += " ,NULLIF('" + marketCap + "','')";
                query += " ,NULLIF('" + stckAmt + "','')";
                query += " ,NULLIF('" + per + "','')";
                query += " ,NULLIF('" + eps + "','')";
                query += " ,NULLIF('" + roe + "','')";
                query += " ,NULLIF('" + pbr + "','')";
                query += " ,NULLIF('" + ebit + "','')";
                query += " , 'P'";
                query += " , NOW()";
                query += " )";
                query += " ON DUPLICATE KEY UPDATE ITEM_NM          = SUBSTR('" + itemNm + "', 1, 50)";
                query += "                       , PRICE            = NULLIF('" + endPrice + "','')";
                query += "                       , PRC_DIFF         = NULLIF('" + diff + "','')";
                query += "                       , RATE             = NULLIF('" + rate + "','')";
                query += "                       , MARKET_CAP       = NULLIF('" + marketCap + "','')";
                query += "                       , SHARE_NUM        = NULLIF('" + stckAmt + "','')";
                query += "                       , PER              = NULLIF('" + per + "','')";
                query += "                       , EPS              = NULLIF('" + eps + "','')";
                query += "                       , ROE              = NULLIF('" + roe + "','')";
                query += "                       , PBR              = NULLIF('" + pbr + "','')";
                query += "                       , EBIT             = NULLIF('" + ebit + "','')";
                query += "                       , WORK_MAN         = 'P'";
                query += "                       , WORK_TIME        = NOW()";

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "종목코드 :{0}, 건수 1 : {1}, 건수 2 : {2}", item, nCnt0, nCnt1);
            }
        }


        // 주식멀티현재가조회
        public void ApiT8407(string reqName, string items, int reqCnt)
        {
            t8407.ResFileName = "C:\\eBest\\xingAPI\\Res\\t8407.res";
            t8407.SetFieldData("t8407InBlock", "nrec", 0, reqCnt.ToString());
            t8407.SetFieldData("t8407InBlock", "shcode", 0, items.Replace(";", ""));

            ApiRequest(t8407, false, reqName);
        }
        public void ApiT8407R(string trCode)
        {
            string blockNm = "t8407OutBlock1";
            int nCnt = t8407.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string item = t8407.GetFieldData(blockNm, "shcode", i);
                string itemNm = t8407.GetFieldData(blockNm, "hname", i);
                int price = Math.Abs(Int32.Parse(t8407.GetFieldData(blockNm, "price", i)));
                int highPrice = Math.Abs(Int32.Parse(t8407.GetFieldData(blockNm, "high", i)));

                Dictionary<string, object> row = new Dictionary<string, object>();
                row.Add("item", item);
                row.Add("itemNm", itemNm);
                row.Add("price", price);
                row.Add("highPrice", highPrice);

                rslt.Add(row);

                _formObj.Logger(Log.조회, "종목코드 :{0} | 종목명 :{1} | 현재가 :{2} | 고가 :{3}", item, itemNm, price, highPrice);
            }

            _formObj._sttg.RetInterestItem_Callback(rslt);
        }


        // 주식멀티호가조회
        public void ApiT8407hoga(string reqName, string items, int reqCnt)
        {
            t8407hoga.ResFileName = "C:\\eBest\\xingAPI\\Res\\t8407.res";
            t8407hoga.SetFieldData("t8407InBlock", "nrec", 0, reqCnt.ToString());
            t8407hoga.SetFieldData("t8407InBlock", "shcode", 0, items.Replace(";", ""));

            ApiRequest(t8407hoga, false, reqName);
        }
        public void ApiT8407Rhoga(string trCode)
        {
            string blockNm = "t8407OutBlock1";
            int nCnt = t8407hoga.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string item = t8407hoga.GetFieldData(blockNm, "shcode", i);
                string itemNm = t8407hoga.GetFieldData(blockNm, "hname", i);
                int price = Math.Abs(Int32.Parse(t8407hoga.GetFieldData(blockNm, "price", i)));
                int sellHo = Math.Abs(Int32.Parse(t8407hoga.GetFieldData(blockNm, "offerho", i))); //매도호가
                int buyHo = Math.Abs(Int32.Parse(t8407hoga.GetFieldData(blockNm, "bidho", i))); //매수호가

                string query = "";
                query += " UPDATE POOL";
                query += "    SET BEGIN_PRICE         = '" + sellHo + "'";
                query += "      , BUY_INVOLVE_ITEM_YN = 'Y'";
                query += "      , BEGINPRICE_TIME     = NOW()";
                query += "  WHERE TRAN_DAY = (SELECT MAX(A.TRAN_DAY)";
                query += "                      FROM (SELECT * FROM POOL) A";
                query += "                     WHERE A.TRAN_DAY < DATE_FORMAT(NOW(), '%Y%m%d')";
                query += "                   )";
                query += "    AND ITEM     = '" + item + "'";

                _formObj._db.InsertQuery(query);

                _formObj.Logger(Log.조회, "종목코드 :{0} | 종목명 :{1} | 현재가 :{2} | 매도호가 :{3} | 매수호가 : {4}", item, itemNm, price, sellHo, buyHo);
            }
        }


        // 주식차트(일) - 이동평균 및 이격도 계산하기위한 기초 가격
        public void ApiT8413(string reqName, string item, string sdt, string edt)
        {
            t8413.ResFileName = "C:\\eBest\\xingAPI\\Res\\t8413.res";
            t8413.SetFieldData("t8413InBlock", "shcode", 0, item);
            t8413.SetFieldData("t8413InBlock", "gubun", 0, "2"); //2:일,3:주,4:월
            t8413.SetFieldData("t8413InBlock", "qrycnt", 0, "5");
            t8413.SetFieldData("t8413InBlock", "sdate", 0, sdt);
            t8413.SetFieldData("t8413InBlock", "edate", 0, edt);
            t8413.SetFieldData("t8413InBlock", "cts_date", 0, edt);
            t8413.SetFieldData("t8413InBlock", "comp_yn", 0, "N"); // 압축여부

            ApiRequest(t8413, false, reqName);
        }
        public void ApiT8413R(string trCode)
        {
            string item = t8413.GetFieldData("t8413OutBlock", "shcode", 0);

            string blockNm = "t8413OutBlock1";
            int nCnt = t8413.GetBlockCount(blockNm);      

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string yyyymmdd = t8413.GetFieldData(blockNm, "date", i);
                double tranAmt = Math.Abs(Double.Parse(t8413.GetFieldData(blockNm, "value", i).Trim()));
                double tranTot = Math.Abs(Double.Parse(t8413.GetFieldData(blockNm, "jdiff_vol", i).Trim()));
                int endPrice = Math.Abs(Int32.Parse(t8413.GetFieldData(blockNm, "close", i).Trim()));
                int beginPrice = Math.Abs(Int32.Parse(t8413.GetFieldData(blockNm, "open", i).Trim()));
                int highPrice = Math.Abs(Int32.Parse(t8413.GetFieldData(blockNm, "high", i).Trim()));
                int lowPrice = Math.Abs(Int32.Parse(t8413.GetFieldData(blockNm, "low", i).Trim()));

                _formObj.Logger(Log.전략, "[이동평균 및 이격도 조회(장기) 콜백] 종목코드 : {0}, 일자 : {1}, 현재가 : {2}", item, yyyymmdd, endPrice);

                string query = "";
                query += " INSERT INTO PRICE";
                query += "      ( ITEM";
                query += "      , TRAN_DAY";
                query += "      , BEGIN";
                query += "      , END";
                query += "      , HIGH";
                query += "      , LOW";
                query += "      , TRAN_TOT";
                query += "      )";
                query += " VALUES";
                query += "      ( '" + item + "'";
                query += "      , '" + yyyymmdd + "'";
                query += "      , " + beginPrice;
                query += "      , " + endPrice;
                query += "      , " + highPrice;
                query += "      , " + lowPrice;
                query += "      , " + tranTot;
                query += "      )";
                query += " ON DUPLICATE KEY UPDATE BEGIN = " + beginPrice;
                query += "                       , END   = " + endPrice;
                query += "                       , HIGH  = " + highPrice;
                query += "                       , LOW   = " + lowPrice;
                query += "                       , TRAN_TOT = " + tranTot;

                _formObj._db.InsertQuery(query);
            }
        }


        // 외인기관별종목별동향
        public void ApiT1717(string reqName, string item, string sdt, string edt)
        {
            t1717.ResFileName = "C:\\eBest\\xingAPI\\Res\\t1717.res";
            t1717.SetFieldData("t1717InBlock", "shcode", 0, item);
            t1717.SetFieldData("t1717InBlock", "gubun", 0, "0");
            t1717.SetFieldData("t1717InBlock", "fromdt", 0, sdt);
            t1717.SetFieldData("t1717InBlock", "todt", 0, edt);

            ApiRequest(t1717, false, reqName);
        }
        public void ApiT1717R(string trCode)
        {
            string item = t1717.GetFieldData("t1717InBlock", "shcode", 0);

            string blockNm = "t1717OutBlock";
            int nCnt = t1717.GetBlockCount(blockNm);

            ArrayList rslt = new ArrayList();
            for (int i = 0; i < nCnt; i++)
            {
                string yyyymmdd = t1717.GetFieldData(blockNm, "date", i);
                int tranAmt = Math.Abs(Int32.Parse(t1717.GetFieldData(blockNm, "volume", i))); // 누적거래량
                int ant = Math.Abs(Int32.Parse(t1717.GetFieldData(blockNm, "tjj0008_vol", i)));
                int fore = Math.Abs(Int32.Parse(t1717.GetFieldData(blockNm, "tjj0016_vol", i)));
                int ins = Math.Abs(Int32.Parse(t1717.GetFieldData(blockNm, "tjj0018_vol", i)));

                _formObj.Logger(Log.전략, "[종목별투자자 조회 콜백] 종목코드 : {0}, 일자 : {1}, 누적거래량 : {2}", item, yyyymmdd, tranAmt);

                string query = "";
                query += " INSERT INTO INVESTOR";
                query += "      ( ITEM";
                query += "      , TRAN_DAY";
                query += "      , ACCM_AMT";
                query += "      , ANT";
                query += "      , FORE";
                query += "      , INS";
                query += "      )";
                query += " VALUES";
                query += "      ( '" + item + "'";
                query += "      , '" + yyyymmdd + "'";
                query += "      , " + tranAmt;
                query += "      , " + ant;
                query += "      , " + fore;
                query += "      , " + ins;
                query += "      )";
                query += " ON DUPLICATE KEY UPDATE ACCM_AMT = " + tranAmt;
                query += "                       , ANT      = " + ant;
                query += "                       , FORE     = " + fore;
                query += "                       , INS      = " + ins;

                _formObj._db.InsertQuery(query);
            }
        }
    }
}
