using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMWJ
{
    public class Constants
    {
        public static string ACCT   = "5035545411"; // 실계좌번호
        //public static string ACCT = "8064870611"; // 모의계좌번호

        public static string CONN_ADDR = "hts.ebestsec.co.kr";
        public static int CONN_PORT = 20001;
        
        public static string ID = "enhanjh";
        public static string PW = "sens!$33";
        public static string CP = "lmtls1433))";

        public static int SLEEP_TIME = 2999;

        public static double FEE = 0.00015;

        public static int BUY_RATIO_1_2 = 0; // 전략별 투자 비율 1(전일상한가), 2(전일 쌍매수(시가매수))
        public static int BUY_RATIO_3   = 0; // 전략별 투자 비율 3(거래량 급등종목)
        public static int BUY_RATIO_4   = 0; // 전략별 투자 비율 4(당일 쌍매수(종가매수))
        public static int BUY_RATIO_6   = 0; // 전략별 투자 비율 6(볼린져밴드 하단 및 쌍매수)
        public static int BUY_RATIO_7 = 100; // 전략별 투자 비율 7(쌍매수 종목을 5일 후 매도)

        public static int BUY_RATIO_5   = 100; // 전략별 투자 비율 5(볼린져밴드 하단 돌파, 자동추매)

        // 화면번호
        // 1000 : 조회
        // 2000 : 요청
        // 1000 : 일반조회
        // 1100 : 전략조회
        // 1200 : 매수관련조회
        // 1300 : 매도관련조회
        public static string RET_GI             = "1001"; // 단순 기본 정보 조회
        public static string RET_ACCT_BAL       = "1002"; // 계좌잔고 조회
        public static string RET_ITEM_MST       = "1003"; // 종목코드 마스터 조회
        public static string RET_REALTIME_PRICE = "1004"; // 실시간 가격 조회
        public static string RET_INTEREST_ITEM  = "1005"; // 관심종목 조회  

        public static string RET_ULP            = "1101"; // 상한가 조회
        public static string RET_RECENT_ULP     = "1102"; // 최근 상한가 조회
        public static string RET_DUAL_PULLING_S = "1103"; // 기관, 외인 쌍매수(시가) 조회        
        public static string RET_POOL_TRACKING  = "1104"; // 풀 추적 조회        
        public static string COMP_TRAN_AMT      = "1105"; // 전일-당일 거래량 비교
        public static string RET_ALL_ULP        = "1106"; // 상한가 전종목 조회
        public static string RET_TRAN_AMT_SPIKE = "1107"; // 거래량 급등 종목 조회
        public static string RET_AVG_DSP        = "1108"; // 이동평균 및 이격도 조회(당일)  
        public static string RET_POOL_TRACKING_EVID = "1109"; // 풀 추적 조회(evid)
        public static string RET_DUAL_PULLING_E = "1110"; // 기관, 외인 쌍매수(종가) 조회
        public static string RET_ITEM_INVST     = "1111"; // 종목별투자자 조회(당일)
        public static string RET_ITEM_INVST_L   = "1112"; // 종목별투자자 조회(장기)
        public static string RET_AVG_DSP_L      = "1113"; // 이동평균 및 이격도 조회(장기)

        public static string RET_NEAR_START_PRC = "1201"; // 장 시작 직전가 조회(매수비율 계산용)

        public static string RET_ACCT_PROFIT    = "1301"; // 계좌 잔고 수익률 조회
        public static string RET_ACCT_ITEM_SALE = "1302"; // 전량 매도를 위한 계좌 잔고 종목 조회
        public static string RET_ACCT_ITEM      = "1303"; // 계좌 잔고 종목 조회
        public static string RET_SELL_DET_INFO  = "1304"; // 매도 판단 정보 조회

        public static string REQ_BUY            = "2202"; // 매수
        public static string REQ_BUY_ADD        = "2201"; // 추매
    }
}
