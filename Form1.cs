using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Coursework_chem
{

    public partial class Form1 : Form
    {
        //Список названий солей
        List<string> listOfNameSalt = new List<string>();

        double a0, a1, a2, d0, d1, d2, B0, Bx, Bt, B2;


        public Form1()
        {
            InitializeComponent();
            saltList.Items.Clear();

            //считываем соли и выводим их списком
            using (var reader = new StreamReader(@"./data/Salt.csv"))
            {

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');

                    listOfNameSalt.Add(values[0]);

                    saltList.Items.Add(values[0]);
                }

            }
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {

            //первичные вычисления
            calcMassFraction();
            calcMassConcentration();

            //вводим данные из таблиц
            inputData();

            //начинаем вычислять
            calcAndDraw();
        }

        //Ввод данных о соли в словарь
        private void inputData()
        {
            int indexOfSalt = listOfNameSalt.IndexOf(saltList.Text);
            string line = "";//Хранит полный список свойств солей через ;
            string[] values = { };

            using (var reader = new StreamReader(@"./data/Salt.csv"))
            {
                for (int i = 0; i <= indexOfSalt; i++)
                {
                    line = reader.ReadLine();
                }
                values = line.Split(';');
            }
            //Заносим значения в словарь
            //1-я таблица
            a0 = Double.Parse(values[1]);
            a1 = Double.Parse(values[2]);
            a2 = Double.Parse(values[3]);
            //2-я табица
            d0 = Double.Parse(values[4]);
            d1 = Double.Parse(values[5]);
            d2 = Double.Parse(values[6]);
            //3-я таблица
            B0 = Double.Parse(values[7]);
            Bx = Double.Parse(values[8]);
            Bt = Double.Parse(values[9]);
            B2 = Double.Parse(values[10]);

            a0 *= 0.0001;
            a1 *= 0.000001;
            a2 *= 0.00000001;

            d0 *= 0.01;
            d1 *= 0.0001;
            d2 *= 0.000000001;

            B2 *= 0.001;
        }

        //============================================= Первичные расчеты

        //массовая доля в растворе
        private double calcMassFraction()
        {
            var massFraction = massSalt.Value / (massSalt.Value + massH2O.Value);
            tbMassFraction.Text = (Math.Round(massFraction, 8)).ToString();
            return (double)massFraction;
        }
        //массовая концентрация
        private double calcMassConcentration()
        {
            var massConcentration = (massSalt.Value / massH2O.Value);
            tbMassConcentration.Text = (Math.Round(massConcentration, 8)).ToString();
            return (double)massConcentration;
        }

        //============================================= Вторичные расчеты

        //плотность выды при заданной температуре p_H2O
        private double densityFromTemp(double temp)
        {
            using (var reader = new StreamReader(@"./data/DensityH2O.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    //val[0] - температура; val[1] - значение
                    if (Double.Parse(values[0]) >= temp)
                    {
                        return Double.Parse(values[1]);
                    }
                }
            }
            //1 - плотность при т = 0
            return 1;
        }

        //вязкость воды при заданной т - u_H20(t)
        private double viscosityFromTemp(double temp)
        {
            return 0.59849 * Math.Pow(43.252 + temp, -1.5423);
        }

        //удельная теплоёмкость воды C_H2O(t)
        private double water_capacity(double temp)
        {
            if(temp <= 0) { temp = 0.00000001; }
            return 4223.6 + 2.476 * temp * Math.Log10(temp / 100);
        }

        //============================================= Конечные расчеты + построение графиков
        private void calcAndDraw()
        {
            chart_density.Series[0].Points.Clear();
            chart_viscosity.Series[0].Points.Clear();
            chart_thermal_conductivity.Series[0].Points.Clear();
            dataGridView1.Rows.Clear();

            for (var t = temperature0.Value; t < temperatureN.Value; t++)
            {
                var d = Math.Round(calcDensity((double)t), 4);
                var v = Math.Round(calcViscosity((double)t), 4);
                var tc = Math.Round(calcThermalConductivity((double)t), 4);

                chart_density.Series[0].Points.AddXY((double)t, d);
                chart_viscosity.Series[0].Points.AddXY((double)t, v);
                chart_thermal_conductivity.Series[0].Points.AddXY((double)t, tc);

                dataGridView1.Rows.Add(t, d, v, tc);
            }
        }
        private double calcDensity(double t)
        {
            //плотность чистой воды т-плотность
            //0-999,8
            //10-999,7
            //20-998,2
            //50-988

            double first = Math.Log10(densityFromTemp((double)t));
            double second = a0 + a1 * t  + a2 * Math.Pow(t, 2);
            double x = calcMassConcentration();

            double d = first + second * x;

            //d = Math.Log10(d);
            d = Math.Pow(10, d);//Убираем логарифм
            return d * 1000;

        }
        private double calcViscosity(double t)
        {
            //норм значения вязкости чистой воды при т-вязкость
            //0-1788
            //20-1004
            //40-650
            //100-282

            double first = Math.Log10(viscosityFromTemp(t));
            double second = d0 + d1 * t + d2 * Math.Pow(t, 2);
            double x = calcMassConcentration();

            double v = first + second * x;

            //v = Math.Log10(v);
            v = Math.Pow(10, v);//Убираем логарифм

            return v * 1000000;

        }
        private double calcThermalConductivity(double t)
        {
            //норм теплоёмкость т-теплоёмкость
            //0-4217
            //10-4191
            //20-4183
            //50-4181
            //100-4220

            double first = water_capacity(t);
            double second = B0 + Bx * calcMassConcentration() + Bt * t + B2 * Math.Pow(t, 2);
            double x = calcMassConcentration();

            double tc = first + second * x;
            return tc;
        }

    }
}
