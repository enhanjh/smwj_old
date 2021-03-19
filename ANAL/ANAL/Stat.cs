using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SMWJ
{
    class Stat
    {
        // 배열의 평균값을 계산하는 메소드
        public static double mean(List<double> arr, int length)
        {
            double total = 0;
            for (int i = 0; i < length; i++)
            {
                total = total + arr[i];
            }
            return total / length;
        }


        // 배열의 편차값을 구하는 메소드
        public static double variance(List<double> arr, int length)
        {
            return variance(arr, mean(arr, length), length);
        }


        // 배열의 편차값을 구하는 메소드
        public static double variance(List<double> arr, double mean, int length)
        {
            double totalDev = 0;

            for (int i = 0; i < length; i++)
            {
                totalDev = totalDev + (mean - arr[i]) * (mean - arr[i]);
            }

            // Sample estimate of variance so divide by n-1.
            return totalDev / (length - 1);
        }


        // 배열의 표준편차값을 구하는 메소드
        public static double stdDev(double variance)
        {
            return Math.Sqrt(variance);
        }


        // 배열의 표준편차값을 구하는 메소드
        public static double stdDev(List<double> arr, int length)
        {
            return stdDev(variance(arr, length));
        }
    }
}
