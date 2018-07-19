using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace MCNPFileEditor.DataClassAndControl
{
    public class OutputProperties : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
    
    public class usercodeProperties : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        #region ct protocol
        private decimal _ct_scanner_initial_rotation_angle_in_degree = 0;

        public decimal ct_scanner_initial_rotation_angle_in_degree
        {
            set
            {
                _ct_scanner_initial_rotation_angle_in_degree = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_initial_rotation_angle_in_degree"));
            }
            get { return _ct_scanner_initial_rotation_angle_in_degree; }
        }

        private decimal _ct_scanner_rotation_angle_delta_in_degree = 22.5M;

        public decimal ct_scanner_rotation_angle_delta_in_degree
        {
            set
            {
                _ct_scanner_rotation_angle_delta_in_degree = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_rotation_angle_delta_in_degree"));
            }
            get
            {
                return _ct_scanner_rotation_angle_delta_in_degree;
            }
        }

        private decimal _ct_scanner_z_initial_translation = 0;

        public decimal ct_scanner_z_initial_translation
        {
            set
            {
                _ct_scanner_z_initial_translation = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_z_initial_translation"));
            }
            get { return _ct_scanner_z_initial_translation; }
        }

        private String _ct_scanner_motion_type = "axial";

        public string ct_scanner_motion_type
        {
            set
            {
                _ct_scanner_motion_type = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_motion_type"));
            }
            get { return _ct_scanner_motion_type; }
        }

        private string _ct_dose_print_granularity = "custom-num-projection";

        public string ct_dose_print_granularity
        {
            set
            {
                _ct_dose_print_granularity = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_dose_print_granularity"));
            }
            get { return _ct_dose_print_granularity; }
        }

        private int _ct_scanner_custom_num_projection = 16;

        public int ct_scanner_custom_num_projection
        {
            set
            {
                _ct_scanner_custom_num_projection = value;
                ct_scanner_rotation_angle_delta_in_degree = (decimal)(360.0 / _ct_scanner_custom_num_projection);
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_custom_num_projection"));
            }
            get { return _ct_scanner_custom_num_projection; }
        }

        private int _universe_ct_scanner = 99998;

        public int universe_ct_scanner
        {
            set
            {
                _universe_ct_scanner = value;
                OnPropertyChanged(new PropertyChangedEventArgs("universe_ct_scanner"));
            }
            get { return _universe_ct_scanner; }
        }
        #endregion

        #region run

        private long _num_history = 1000000;

        public long num_history
        {
            set
            {
                _num_history = value;
                OnPropertyChanged(new PropertyChangedEventArgs("num_history"));
            }
            get { return _num_history; }
        }

        private long _cylinder_bound_radius = 100;

        public long cylinder_bound_radius
        {
            set
            {
                _cylinder_bound_radius = value;
                OnPropertyChanged(new PropertyChangedEventArgs("cylinder_bound_radius"));
            }
            get { return _cylinder_bound_radius; }
        }

        private long _cylinder_bound_z_upper  = 200;

        public long cylinder_bound_z_upper
        {
            set
            {
                _cylinder_bound_z_upper = value;
                OnPropertyChanged(new PropertyChangedEventArgs("cylinder_bound_z_upper"));
            }
            get { return _cylinder_bound_z_upper; }
        }

        private long _cylinder_bound_z_lower = -20;

        public long cylinder_bound_z_lower
        {
            set
            {
                _cylinder_bound_z_lower = value;
                OnPropertyChanged(new PropertyChangedEventArgs("cylinder_bound_z_lower"));
            }
            get { return _cylinder_bound_z_lower; }
        }

        private long _universe_air = 99999;

        public long universe_air
        {
            set
            {
                _universe_air = value;
                OnPropertyChanged(new PropertyChangedEventArgs("universe_air"));
            }
            get { return _universe_air; }
        }

        #endregion

        /// <summary>
        /// 输出到文件
        /// </summary>
        /// <param name="outputPath">输出文件的路径，相对或绝对</param>
        public void Output(string outputPath)
        {
            try
            {
                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    using (TextWriter tw = new StreamWriter(fs))
                    {
                        tw.Write("c ------------------------------------------------------------" + '\n');
                        tw.Write("c   ct protocol" + '\n');
                        tw.Write("c ------------------------------------------------------------" + '\n');
                        tw.Write(string.Format("ct-scanner-initial-rotation-angle-in-degree  = {0:F}",
                            _ct_scanner_initial_rotation_angle_in_degree) + '\n');
                        tw.Write(string.Format("ct-scanner-rotation-angle-delta-in-degree    = {0:F}",
                            _ct_scanner_rotation_angle_delta_in_degree) + '\n');
                        tw.Write(string.Format("ct-scanner-z-initial-translation             = {0:F}",
                            _ct_scanner_z_initial_translation) + '\n');
                        tw.Write(string.Format("ct-scanner-motion-type                       = " +
                                                   _ct_scanner_motion_type) + '\n');
                        tw.Write(string.Format("ct-dose-print-granularity                    = " +
                                                   _ct_dose_print_granularity) + '\n');
                        tw.Write(string.Format("ct-scanner-custom-num-projection             = " +
                                                   _ct_scanner_custom_num_projection) + '\n');
                        tw.Write(string.Format("universe-ct-scanner                          = " +
                                                   _universe_ct_scanner) + '\n');
                        tw.Write("c ------------------------------------------------------------" + '\n');
                        tw.Write("c   run" + '\n');
                        tw.Write("c ------------------------------------------------------------" + '\n');
                        tw.Write(string.Format("num-history                                  = " +
                                                   _num_history) + '\n');
                        tw.Write(string.Format("cylinder-bound-radius                        = " +
                                                   _cylinder_bound_radius) + '\n');
                        tw.Write(string.Format("cylinder-bound-z-upper                       = " +
                                                   _cylinder_bound_z_upper) + '\n');
                        tw.Write(string.Format("cylinder-bound-z-lower                       = " +
                                                   _cylinder_bound_z_lower) + '\n');
                        tw.Write(string.Format("universe-air                                 = " +
                                                   universe_air) + '\n');
                    }
                }
            }
            catch (Exception)
            {
                //
            }
        }
    }

    public class outputFileName : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private string materialFilePath = "material.txt";

        public string MaterialFilePath
        {
            set
            {
                materialFilePath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("MaterialFilePath"));
            }
            get { return materialFilePath; }
        }

        private string tallyFilePath = "tally";

        public string TallyFilePath
        {
            set
            {
                tallyFilePath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TallyFilePath"));
            }
            get { return tallyFilePath; }
        }

        private string _universe_to_materialFilePath = "universe_to_material";

        public string universe_to_materialFilePath
        {
            set
            {
                _universe_to_materialFilePath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("universe_to_materialFilePath"));
            }
            get { return _universe_to_materialFilePath; }
        }

        private string usercodeFilePath = "usercode.txt";

        public string UsercodeFilePath
        {
            set
            {
                usercodeFilePath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("UsercodeFilePath"));
            }
            get { return usercodeFilePath; }
        }

        private string runshFilePath = "run.sh";

        public string RunshFilePath
        {
            set
            {
                runshFilePath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("RunshFilePath"));
            }
            get { return runshFilePath; }
        }
    }

    public class runshParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private decimal _ct_scanner_z_translation_delta = 2M;

        public decimal ct_scanner_z_translation_delta
        {
            set
            {
                _ct_scanner_z_translation_delta = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ct_scanner_z_translation_delta"));
            }
            get { return _ct_scanner_z_translation_delta; }
        }

        private int _num_thread_cpu = 48;

        public int num_thread_cpu
        {
            set
            {
                _num_thread_cpu = value;
                OnPropertyChanged(new PropertyChangedEventArgs("num_thread_cpu"));
            }
            get { return _num_thread_cpu; }
        }

        private bool shouldOutputGPUScript = false;

        public bool ShouldOutputGPUScript
        {
            set
            {
                shouldOutputGPUScript = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ShouldOutputGPUScript"));
            }
            get { return shouldOutputGPUScript; }
        }

        private string _gpu_to_use = "0";

        public string gpu_to_use
        {
            set
            {
                _gpu_to_use = value;
                OnPropertyChanged(new PropertyChangedEventArgs("gpu_to_use"));
            }
            get { return _gpu_to_use; }
        }
        
        // 使用光谱
        private int spectrum = 120;

        public int Spectrum
        {
            set
            {
                spectrum = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Spectrum"));
            }
            get { return spectrum; }
        }

        // 使用Scanner
        private string scanner = "20-body";

        public string Scanner
        {
            set
            {
                scanner = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Scanner"));
            }
            get { return scanner; }
        }
    }

    public static class OutputInneedParameter
    {
        public static string F4TallyCards =
            @"
organ-dose-start 14
f4-p
(f4+de/df) humeri, upper half, spongiosa   = 14
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 24
f4-p
(f4+de/df) clavicles, spongiosa            = 25
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 34
f4-p
(f4+de/df) cranium, spongiosa              = 27
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 44
f4-p
(f4+de/df) femora, upper half, spongiosa   = 29
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 54
f4-p
(f4+de/df) mandible, spongiosa             = 40
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 64
f4-p
(f4+de/df) pelvis, spongiosa               = 42
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 74
f4-p
(f4+de/df) ribs, spongiosa                 = 44
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 84
f4-p
(f4+de/df) scapulae, spongiosa             = 46
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 94
f4-p
(f4+de/df) cervical spine, spongiosa       = 48
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 104
f4-p
(f4+de/df) thoracic spine, spongiosa       = 50
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 114
f4-p
(f4+de/df) lumbar spine, spongiosa         = 52
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 124
f4-p
(f4+de/df) sacrum, spongiosa               = 54
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 134
f4-p
(f4+de/df) sternum, spongiosa              = 56
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end





organ-dose-start 144
f4-p
(f4+de/df) all                             = 14 25 27 29 40 42 44 46 48 50 52 54 56
de    0.001  0.002  0.004  0.006  0.008  0.01  0.015  0.02  0.03        0.04  0.05  0.06  0.08  0.1  0.12  0.14  0.15  0.2  0.3  0.4        0.5  0.6  0.8  1  2  3  4  5  6  8  10
df    3.087265918  0.915917603  0.247515605  0.108676654  0.06119226        0.038308365  0.016111111  0.008689139  0.003876404  0.002521848        0.002059925  0.001910112  0.002122347  0.002602996  0.003189763        0.003801498  0.004138577  0.005942572  0.009606742  0.013158552        0.016548065  0.019769039  0.025736579  0.03113608  0.052297129        0.068489388  0.082440699  0.095262172  0.107434457  0.130667915        0.153426966
organ-dose-end
";
    }

    public class UsercodeInneedParameter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private static ObservableCollection<string> ct_scanner_motion_type_Candidate = new ObservableCollection<string>()
        {
            "helical",
            "axial"
        };
    }
}
