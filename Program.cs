using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ellipse = System.Windows.Shapes.Ellipse;
using System.Windows.Markup;
using System.Windows.Interop;
using System.Xml;
using Diahon.WinUI.Printing.Wpf;
using Microsoft.Win32;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.Storage.Streams;

namespace FFB0Scale
{
    public sealed class ProfileSettings
    {
        public string height_cm { get; set; }
        public string age { get; set; }
        public string sex { get; set; }
        public bool athlete { get; set; }
        public string address { get; set; }
        public ProfileSettings() { height_cm = ""; age = ""; sex = "Male"; address = ""; }
    }

    public sealed class UserProfile
    {
        public double HeightCm;
        public int Age;
        public bool Male;
        public bool Athlete;
    }

    public class MeasurementResult
    {
        public double weight_kg { get; set; }
        public double bmi { get; set; }
        public double body_fat_percent { get; set; }
        public double subcutaneous_fat_percent { get; set; }
        public double visceral_fat { get; set; }
        public double muscle_percent { get; set; }
        public double skeletal_muscle_percent { get; set; }
        public double body_water_percent { get; set; }
        public double protein_percent { get; set; }
        public double bone_mass_kg { get; set; }
        public double bmr_kcal { get; set; }
        public double metabolic_age { get; set; }
        public double body_score { get; set; }
        public double heart_rate_bpm { get; set; }
        public double impedance_ohm { get; set; }
    }

    public sealed class HistoryRecord : MeasurementResult
    {
        public string measured_at { get; set; }
        public string patient_name { get; set; }
        public double height_cm { get; set; }
        public int age { get; set; }
        public string sex { get; set; }
        public bool athlete { get; set; }
        public string DateDisplay { get { DateTime d; return DateTime.TryParse(measured_at, null, DateTimeStyles.RoundtripKind, out d) ? d.ToLocalTime().ToString("dd.MM.yyyy, HH:mm",new CultureInfo("ro-RO")) : measured_at; } }
        public string WeightDisplay { get { return weight_kg.ToString("F2") + " kg"; } }
        public string BmiDisplay { get { return bmi.ToString("F1"); } }
        public string FatDisplay { get { return body_fat_percent.ToString("F1") + "%"; } }
        public string WaterDisplay { get { return body_water_percent.ToString("F1") + "%"; } }
        public string MuscleDisplay { get { return muscle_percent.ToString("F1") + "%"; } }
        public string PulseDisplay { get { return heart_rate_bpm.ToString("F0") + " bpm"; } }

        public static HistoryRecord FromResult(MeasurementResult r)
        {
            return new HistoryRecord {
                measured_at = DateTime.UtcNow.ToString("o"), weight_kg = r.weight_kg, bmi = r.bmi,
                body_fat_percent = r.body_fat_percent, subcutaneous_fat_percent = r.subcutaneous_fat_percent,
                visceral_fat = r.visceral_fat, muscle_percent = r.muscle_percent,
                skeletal_muscle_percent = r.skeletal_muscle_percent, body_water_percent = r.body_water_percent,
                protein_percent = r.protein_percent, bone_mass_kg = r.bone_mass_kg, bmr_kcal = r.bmr_kcal,
                metabolic_age = r.metabolic_age, body_score = r.body_score,
                heart_rate_bpm = r.heart_rate_bpm, impedance_ohm = r.impedance_ohm
            };
        }

        public static HistoryRecord FromResult(MeasurementResult r, string patientName, UserProfile p)
        {
            var x=FromResult(r); x.patient_name=patientName; x.height_cm=p.HeightCm; x.age=p.Age;
            x.sex=p.Male?"Masculin":"Feminin"; x.athlete=p.Athlete; return x;
        }

        public MeasurementResult ToResult()
        {
            return new MeasurementResult {
                weight_kg = weight_kg, bmi = bmi, body_fat_percent = body_fat_percent,
                subcutaneous_fat_percent = subcutaneous_fat_percent, visceral_fat = visceral_fat,
                muscle_percent = muscle_percent, skeletal_muscle_percent = skeletal_muscle_percent,
                body_water_percent = body_water_percent, protein_percent = protein_percent,
                bone_mass_kg = bone_mass_kg, bmr_kcal = bmr_kcal, metabolic_age = metabolic_age,
                body_score = body_score, heart_rate_bpm = heart_rate_bpm, impedance_ohm = impedance_ohm
            };
        }
    }

    public sealed class PatientSuggestion
    {
        public string Name { get; set; }
        public string Detail { get; set; }
        public HistoryRecord Record { get; set; }
    }

    public sealed class Assessment
    {
        public string Label;
        public string Reference;
        public string Tone;
        public Assessment(string label, string reference, string tone) { Label = label; Reference = reference; Tone = tone; }
    }

    public sealed class BarSpec
    {
        public double Min;
        public double Max;
        public double[] Breaks;
        public Color[] Colors;
        public string MinLabel;
        public string MaxLabel;
    }

    public sealed class ScaleBar : FrameworkElement
    {
        private double _value;
        private BarSpec _spec;
        public ScaleBar() { Height = 36; MinWidth = 120; }
        public void Configure(double value, BarSpec spec) { _value = value; _spec = spec; InvalidateVisual(); }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width, 36);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double width = Math.Max(ActualWidth, 80);
            double left = 2, right = width - 2, top = 5, height = 9;
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(230,235,241)), null, new Rect(left,top,right-left,height), 5,5);
            if (_spec == null) return;
            double span = Math.Max(_spec.Max - _spec.Min, .001);
            double[] bounds = new double[_spec.Breaks.Length + 2]; bounds[0] = _spec.Min; bounds[bounds.Length-1] = _spec.Max;
            Array.Copy(_spec.Breaks, 0, bounds, 1, _spec.Breaks.Length);
            dc.PushClip(new RectangleGeometry(new Rect(left,top,right-left,height),5,5));
            for (int i=0; i<_spec.Colors.Length && i<bounds.Length-1; i++)
            {
                double x = left + (bounds[i]-_spec.Min)/span*(right-left);
                double x2 = left + (bounds[i+1]-_spec.Min)/span*(right-left);
                dc.DrawRectangle(new SolidColorBrush(_spec.Colors[i]), null, new Rect(x,top,Math.Max(0,x2-x),height));
            }
            dc.Pop();
            double ratio = Math.Max(0, Math.Min(1, (_value-_spec.Min)/span));
            double marker = left + ratio*(right-left);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(20,32,51)), null, new Rect(marker-1,top-4,2,height+8));
            var triangle = new StreamGeometry(); using (var c=triangle.Open()) { c.BeginFigure(new Point(marker-4,top-5),true,true); c.LineTo(new Point(marker+4,top-5),true,false); c.LineTo(new Point(marker,top),true,false); }
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(20,32,51)),null,triangle);
            var typeface = new Typeface("Segoe UI");
            var muted = new SolidColorBrush(Color.FromRgb(125,138,154));
            var a = new FormattedText(_spec.MinLabel,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,typeface,9,muted);
            var b = new FormattedText(_spec.MaxLabel,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,typeface,9,muted);
            dc.DrawText(a,new Point(left,21)); dc.DrawText(b,new Point(right-b.Width,21));
        }
    }

    public static class BodyMath
    {
        private static double R1(double v) { return Math.Round(v + 1e-9, 1); }
        public static MeasurementResult Compute(double weight, double impedance, int heartRate, UserProfile p)
        {
            if (impedance < 200 || impedance > 1200) throw new ArgumentException("Implausible impedance reading.");
            double h = p.HeightCm, hm = h/100.0, h2r = h*h/impedance, ffm, waterL;
            if (p.Male) { ffm=-10.68+.65*h2r+.26*weight+.02*impedance; waterL=1.2+.45*h2r+.18*weight; }
            else { ffm=-9.53+.69*h2r+.17*weight+.02*impedance; waterL=3.75+.45*h2r+.11*weight; }
            ffm=Math.Min(Math.Max(ffm,weight*.25),weight*.97);
            double fat=Math.Min(Math.Max((weight-ffm)/weight*100,3),60);
            double waterKg=Math.Min(Math.Max(waterL*.99513,0),ffm);
            double skeletal=.401*h2r+3.825*(p.Male?1:0)-.071*p.Age+5.102;
            skeletal=Math.Min(Math.Max(skeletal,0),ffm);
            double bone=ffm*(p.Male?.057:.05), muscle=Math.Max(0,ffm-bone), protein=Math.Max(0,ffm-waterKg-bone);
            double visceral = p.Male ? p.Age*.15+weight*(-.0015*h+.765)-h*.143-5 : p.Age*.07+weight*(-.0024*h+.691)-h*.027-10.5;
            visceral=Math.Min(Math.Max(visceral,1),50);
            double sub=Math.Min(Math.Max((fat*-.0002+.72)*fat,1),60);
            double score=100-Math.Abs(weight/(hm*hm)-(p.Male?22:21))*2-Math.Abs(fat-(p.Male?15:25)); score=Math.Min(Math.Max(score,0),100);
            return new MeasurementResult {
                weight_kg=Math.Round(weight,3), bmi=R1(weight/(hm*hm)), body_fat_percent=R1(fat), subcutaneous_fat_percent=R1(sub),
                visceral_fat=R1(visceral), muscle_percent=R1(muscle/weight*100), skeletal_muscle_percent=R1(skeletal/weight*100),
                body_water_percent=R1(waterKg/weight*100), protein_percent=R1(protein/weight*100), bone_mass_kg=R1(bone),
                bmr_kcal=Math.Round(370+21.6*ffm), metabolic_age=MetabolicAge(p.Age,fat,p.Male), body_score=R1(score),
                heart_rate_bpm=heartRate, impedance_ohm=R1(impedance)
            };
        }

        private static int MetabolicAge(int age, double fat, bool male)
        {
            double[] ceilings=male?new double[]{14,19,24,27,30,33,36}:new double[]{24,28,32,35,38,42,45};
            int[] offsets=new int[]{-3,-2,-1,1,2,3,4};
            for(int i=0;i<ceilings.Length;i++) if(fat<ceilings[i]) return Math.Max(18,age+offsets[i]);
            return age+5;
        }
    }

    public static class References
    {
        public static readonly Color Low=Color.FromRgb(113,167,232), Good=Color.FromRgb(21,154,104), Caution=Color.FromRgb(228,154,37), Bad=Color.FromRgb(217,84,77), Info=Color.FromRgb(67,133,209), Purple=Color.FromRgb(114,91,196), Gray=Color.FromRgb(138,151,166);
        private static Assessment Band(double v, double[] limits, string[] labels, string[] tones, string reference)
        {
            int i=0; while(i<limits.Length && v>=limits[i]) i++; return new Assessment(labels[i],reference,tones[i]);
        }
        private static string Axis(double v,string unit) { string n=unit=="kg"?v.ToString("F1"):v.ToString("G"); return n+(unit=="%"?"%":unit.Length>0?" "+unit:""); }
        private static BarSpec Bar(double min,double max,double[] breaks,Color[] colors,string unit)
        {
            return new BarSpec{Min=min,Max=max,Breaks=breaks,Colors=colors,MinLabel=Axis(min,unit),MaxLabel=Axis(max,unit)};
        }
        private static double[] FatPoints(UserProfile p)
        {
            if(p.Male) return p.Age<40?new[]{8.0,20,25}:p.Age<60?new[]{11.0,22,28}:new[]{13.0,25,30};
            return p.Age<40?new[]{21.0,33,39}:p.Age<60?new[]{23.0,34,40}:new[]{24.0,36,42};
        }
        private static double[] SkeletalPoints(UserProfile p)
        {
            if(p.Male) return p.Age<40?new[]{33.3,39.4,44.1}:p.Age<60?new[]{33.1,39.2,43.9}:new[]{32.9,39.0,43.7};
            return p.Age<40?new[]{24.3,30.4,35.4}:p.Age<60?new[]{24.1,30.2,35.2}:new[]{23.9,30.0,35.0};
        }
        public static Assessment Assess(string field,double v,MeasurementResult r,UserProfile p)
        {
            if(field=="bmi") return Band(v,new[]{18.5,24.0,28.0,35.0},new[]{"Underweight","Healthy weight","Overweight","Obesity","Severe obesity"},new[]{"caution","good","caution","bad","bad"},"Healthy: 18.5–23.9");
            if(field=="body_fat_percent") { var q=FatPoints(p); if(p.Athlete)q[0]=p.Male?5:14; return Band(v,q,new[]{"Low","Healthy","Elevated","High"},new[]{"caution","good","caution","bad"},"Healthy: "+q[0].ToString("G")+"–"+(q[1]-.1).ToString("F1")+"%"); }
            if(field=="subcutaneous_fat_percent") { var q=p.Male?new[]{8.6,16.8,20.8}:new[]{18.5,26.8,30.8}; return Band(v,q,new[]{"Low","Healthy","Elevated","High"},new[]{"caution","good","caution","bad"},"Healthy: "+q[0].ToString("F1")+"–"+(q[1]-.1).ToString("F1")+"%"); }
            if(field=="body_water_percent") { double lo=p.Male?50:45,hi=p.Male?65.1:60.1; return Band(v,new[]{lo,hi},new[]{"Low","Normal","High"},new[]{"caution","good","info"},"Normal: "+lo.ToString("G")+"–"+(hi-.1).ToString("G")+"%"); }
            if(field=="muscle_percent") { double lo=p.Male?75:65,hi=p.Male?89:79; return Band(v,new[]{lo,hi},new[]{"Low","Normal","High"},new[]{"caution","good","info"},"Reference: "+lo.ToString("G")+"–"+hi.ToString("G")+"%"); }
            if(field=="skeletal_muscle_percent") { var q=SkeletalPoints(p); return Band(v,q,new[]{"Low","Normal","High","Very high"},new[]{"caution","good","info","info"},"Normal: "+q[0].ToString("F1")+"–"+(q[1]-.1).ToString("F1")+"%"); }
            if(field=="protein_percent") return Band(v,new[]{16.0,20.1},new[]{"Low","Normal","High"},new[]{"caution","good","info"},"Normal: 16.0–20.0%");
            if(field=="visceral_fat") return Band(v,new[]{10.0,15.0},new[]{"Healthy","Elevated","High"},new[]{"good","caution","bad"},"Healthy rating: 1–9");
            if(field=="bone_mass_kg") { double e=p.Male?(r.weight_kg<65?2.66:r.weight_kg<=95?3.29:3.69):(r.weight_kg<50?1.95:r.weight_kg<=75?2.4:2.95); return Band(v,new[]{e-.3,e+.3},new[]{"Below reference","Within reference","Above reference"},new[]{"caution","good","info"},"Reference: about "+e.ToString("F1")+" kg"); }
            if(field=="bmr_kcal") return new Assessment("Estimated baseline","No universal healthy band","neutral");
            if(field=="metabolic_age") return v<=p.Age-2?new Assessment("Younger than profile age","Profile age: "+p.Age,"good"):v<p.Age+2?new Assessment("Matches profile age","Profile age: "+p.Age,"good"):new Assessment("Older than profile age","Profile age: "+p.Age,"caution");
            if(field=="body_score") return Band(v,new[]{60.0,80,90},new[]{"Needs attention","Fair","Good","Excellent"},new[]{"bad","caution","good","good"},"App-style score: 0–100");
            if(field=="heart_rate_bpm") return Band(v,new[]{60.0,101},new[]{"Below resting range","Within resting range","Above resting range"},new[]{"caution","good","caution"},"Typical resting: 60–100 bpm");
            return new Assessment("Raw sensor reading","Individual; no health category","neutral");
        }
        public static BarSpec Spec(string field,double v,MeasurementResult r,UserProfile p)
        {
            if(field=="bmi")return Bar(12,40,new[]{18.5,24.0,28,35},new[]{Low,Good,Caution,Color.FromRgb(228,119,62),Bad},"");
            if(field=="body_fat_percent"){var q=FatPoints(p);if(p.Athlete)q[0]=p.Male?5:14;return Bar(0,50,q,new[]{Low,Good,Caution,Bad},"%");}
            if(field=="subcutaneous_fat_percent")return Bar(0,45,p.Male?new[]{8.6,16.8,20.8}:new[]{18.5,26.8,30.8},new[]{Low,Good,Caution,Bad},"%");
            if(field=="body_water_percent")return Bar(30,80,new[]{p.Male?50.0:45,p.Male?65.1:60.1},new[]{Caution,Good,Info},"%");
            if(field=="muscle_percent")return Bar(50,100,new[]{p.Male?75.0:65,p.Male?89.0:79},new[]{Caution,Good,Info},"%");
            if(field=="skeletal_muscle_percent")return Bar(15,60,SkeletalPoints(p),new[]{Caution,Good,Info,Purple},"%");
            if(field=="protein_percent")return Bar(10,30,new[]{16.0,20.1},new[]{Caution,Good,Info},"%");
            if(field=="visceral_fat")return Bar(1,30,new[]{10.0,15},new[]{Good,Caution,Bad},"");
            if(field=="bone_mass_kg"){double e=p.Male?(r.weight_kg<65?2.66:r.weight_kg<=95?3.29:3.69):(r.weight_kg<50?1.95:r.weight_kg<=75?2.4:2.95);return Bar(1,5,new[]{e-.3,e+.3},new[]{Caution,Good,Info},"kg");}
            if(field=="bmr_kcal")return Bar(800,Math.Max(2600,Math.Floor(v*1.25/100)*100+100),new double[0],new[]{Gray},"kcal");
            if(field=="metabolic_age")return Bar(18,Math.Max(80,p.Age+25),new[]{Math.Max(18,p.Age-2.0),p.Age+2.0}.Distinct().Where(x=>x>18&&x<Math.Max(80,p.Age+25)).ToArray(),new[]{Good,Good,Caution},"yr");
            if(field=="body_score")return Bar(0,100,new[]{60.0,80,90},new[]{Bad,Caution,Good,Color.FromRgb(11,124,85)},"");
            if(field=="heart_rate_bpm")return Bar(40,180,new[]{60.0,101},new[]{Caution,Good,Caution},"bpm");
            return Bar(200,1200,new double[0],new[]{Gray},"ohm");
        }
        public static Assessment WeightAssessment(MeasurementResult r,UserProfile p)
        {
            var a=Assess("bmi",r.bmi,r,p); double hm=p.HeightCm/100; return new Assessment(a.Label,"Healthy-weight range: "+(18.5*hm*hm).ToString("F1")+"–"+(23.9*hm*hm).ToString("F1")+" kg",a.Tone);
        }
        public static BarSpec WeightSpec(MeasurementResult r,UserProfile p)
        {
            double h=p.HeightCm/100,h2=h*h; return Bar(12*h2,40*h2,new[]{18.5*h2,24*h2,28*h2,35*h2},new[]{Low,Good,Caution,Color.FromRgb(228,119,62),Bad},"kg");
        }
    }

    public static class DataStore
    {
        public static readonly string LocalDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FFB0 Body Scale");
        public static readonly string SettingsPath=Path.Combine(LocalDir,"settings.json");
        public static readonly string HistoryPath=Path.Combine(LocalDir,"measurements.json");
        private static readonly JsonSerializerOptions JsonOptions=new JsonSerializerOptions{WriteIndented=true};
        public static ProfileSettings LoadSettings()
        {
            Directory.CreateDirectory(LocalDir);
            try { if(File.Exists(SettingsPath)) return JsonSerializer.Deserialize<ProfileSettings>(File.ReadAllText(SettingsPath,Encoding.UTF8),JsonOptions)??new ProfileSettings(); } catch {}
            return new ProfileSettings();
        }
        public static void SaveSettings(ProfileSettings p)
        {
            Directory.CreateDirectory(LocalDir); File.WriteAllText(SettingsPath,JsonSerializer.Serialize(p,JsonOptions),new UTF8Encoding(false));
        }
        public static List<HistoryRecord> LoadHistory()
        {
            Directory.CreateDirectory(LocalDir);
            try { if(File.Exists(HistoryPath)) return JsonSerializer.Deserialize<List<HistoryRecord>>(File.ReadAllText(HistoryPath,Encoding.UTF8),JsonOptions)??new List<HistoryRecord>(); } catch {}
            return new List<HistoryRecord>();
        }
        public static void SaveHistory(List<HistoryRecord> rows)
        {
            Directory.CreateDirectory(LocalDir); File.WriteAllText(HistoryPath,JsonSerializer.Serialize(rows,JsonOptions),new UTF8Encoding(false));
        }
    }

    public sealed class ScaleBluetooth : IDisposable
    {
        private static readonly Guid ServiceId=new Guid("0000ffb0-0000-1000-8000-00805f9b34fb");
        private static readonly Guid WriteId=new Guid("0000ffb1-0000-1000-8000-00805f9b34fb");
        private static readonly Guid NotifyId=new Guid("0000ffb2-0000-1000-8000-00805f9b34fb");
        private static readonly Guid IndicateId=new Guid("0000ffb3-0000-1000-8000-00805f9b34fb");
        private BluetoothLEAdvertisementWatcher _watcher;
        private BluetoothLEDevice _device;
        private GattSession _session;
        private GattDeviceService _service;
        private GattCharacteristic _write,_notify,_indicate;
        private CancellationTokenSource _driveCancel;
        private readonly SemaphoreSlim _writeLock=new SemaphoreSlim(1,1);
        private UserProfile _profile;
        private int _connecting,_sequence,_reply,_controlCount,_sameCount;
        private double _currentWeight,_lastWeight;
        private string _lastResult="";
        public event Action<string> Status;
        public event Action<string> Connected;
        public event Action Disconnected;
        public event Action<double,bool> Weight;
        public event Action<MeasurementResult> Result;
        public event Action<string> Error;

        public void Connect(UserProfile profile)
        {
            if(Interlocked.CompareExchange(ref _connecting,1,0)!=0)return;
            _profile=profile; RaiseStatus("Looking for an FFB0-compatible scale — briefly step on it to wake it…");
            _watcher=new BluetoothLEAdvertisementWatcher(); _watcher.ScanningMode=BluetoothLEScanningMode.Active;
            _watcher.Received+=WatcherReceived;_watcher.Start();
        }

        private async void WatcherReceived(BluetoothLEAdvertisementWatcher sender,BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string name=args.Advertisement.LocalName??""; bool service=args.Advertisement.ServiceUuids.Contains(ServiceId);
            if(!name.Equals("MY_SCALE",StringComparison.OrdinalIgnoreCase)&&!service)return;
            sender.Stop();sender.Received-=WatcherReceived;
            try { await ConnectDevice(args.BluetoothAddress); }
            catch(Exception ex) { Interlocked.Exchange(ref _connecting,0); RaiseError("Could not connect: "+ex.Message); CleanupDevice(); RaiseDisconnected(); }
        }
        private async Task ConnectDevice(ulong address)
        {
            RaiseStatus("Connecting to FFB0 scale…");
            _device=await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();
            if(_device==null)throw new InvalidOperationException("Windows could not open the scale.");
            _session=await GattSession.FromDeviceIdAsync(_device.BluetoothDeviceId).AsTask();
            if(_session==null||!_session.CanMaintainConnection)throw new InvalidOperationException("Windows could not create a persistent GATT session.");
            _session.MaintainConnection=true;
            _service=await FindScaleService(_device);
            _write=await GetCharacteristic(_service,WriteId); _notify=await GetCharacteristic(_service,NotifyId); _indicate=await GetCharacteristic(_service,IndicateId);
            _notify.ValueChanged+=OnValueChanged;_indicate.ValueChanged+=OnValueChanged;
            var ns=await _notify.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask();
            var ins=await _indicate.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask();
            if(ns!=GattCommunicationStatus.Success||ins!=GattCommunicationStatus.Success)throw new InvalidOperationException("Could not enable scale notifications.");
            _device.ConnectionStatusChanged+=DeviceConnectionChanged;
            _sequence=_reply=_controlCount=_sameCount=0; _currentWeight=_lastWeight=0; _lastResult="";
            _driveCancel=new CancellationTokenSource(); Interlocked.Exchange(ref _connecting,0);
            if(Connected!=null)Connected(FormatAddress(address)); RaiseStatus("Connected. Step on barefoot and remain still.");
            Task.Run(()=>DriveLoop(_driveCancel.Token));
        }
        private async Task<GattDeviceService> FindScaleService(BluetoothLEDevice device)
        {
            string diagnostic="";
            for(int attempt=1;attempt<=8;attempt++)
            {
                RaiseStatus("Opening scale service FFB0 (attempt "+attempt+" of 8)… Keep the scale awake.");
                var mode=attempt<=6?BluetoothCacheMode.Uncached:BluetoothCacheMode.Cached;
                var exact=await device.GetGattServicesForUuidAsync(ServiceId,mode).AsTask();
                diagnostic=exact.Status.ToString();
                if(exact.Status==GattCommunicationStatus.Success&&exact.Services.Count>0)return exact.Services[0];
                var all=await device.GetGattServicesAsync(mode).AsTask();
                if(all.Status==GattCommunicationStatus.Success)
                {
                    var match=all.Services.FirstOrDefault(x=>x.Uuid==ServiceId);if(match!=null)return match;
                    diagnostic="services reported: "+string.Join(", ",all.Services.Select(x=>x.Uuid.ToString().Substring(4,4).ToUpperInvariant()));
                }
                else diagnostic=all.Status.ToString();
                await Task.Delay(650+attempt*150);
            }
            throw new InvalidOperationException("Scale service FFB0 was unavailable after several attempts ("+diagnostic+"). Close Fitdays+ on nearby phones and keep one foot on the scale while connecting.");
        }
        private static async Task<GattCharacteristic> GetCharacteristic(GattDeviceService service,Guid id)
        {
            var r=await service.GetCharacteristicsForUuidAsync(id,BluetoothCacheMode.Uncached).AsTask();
            if(r.Status!=GattCommunicationStatus.Success||r.Characteristics.Count==0)throw new InvalidOperationException("Required characteristic "+id.ToString("N").Substring(4,4).ToUpper()+" was unavailable.");
            return r.Characteristics[0];
        }

        private void DeviceConnectionChanged(BluetoothLEDevice sender,object args)
        {
            if(sender.ConnectionStatus==BluetoothConnectionStatus.Disconnected){CleanupDevice();RaiseDisconnected();}
        }
        private void OnValueChanged(GattCharacteristic sender,GattValueChangedEventArgs args)
        {
            byte[] frame; using(var reader=DataReader.FromBuffer(args.CharacteristicValue)){frame=new byte[args.CharacteristicValue.Length];reader.ReadBytes(frame);}
            if(frame.Length!=20||frame[19]!=Checksum(frame))return;
            byte type=frame[3]; if(type==0xA0||type==0xA3)Interlocked.Increment(ref _controlCount);
            if(type==0xA2&&frame[1]>=7)
            {
                double kg=ReadU24(frame,6)/1000.0; bool stable=frame[4]==2||frame[4]==3;
                lock(this){_currentWeight=kg;if(kg>=5&&Math.Abs(kg-_lastWeight)<=.03)_sameCount++;else _sameCount=0;_lastWeight=kg;stable=stable||_sameCount>=8;}
                if(Weight!=null)Weight(kg,stable);
            }
            if(sender==_indicate&&type==0xA3&&frame[1]==8)
            {
                double kg=ReadU24(frame,5)/1000.0; int hr=frame[8]; double imp=(frame[9]<<8)|frame[10]; string key=kg.ToString("F3")+":"+imp+":"+hr;
                lock(this){if(key==_lastResult)return;_lastResult=key;}
                try { var result=BodyMath.Compute(kg,imp,hr,_profile); if(Result!=null)Result(result); }
                catch(Exception ex){RaiseError("BIA calculation failed: "+ex.Message);}
            }
        }

        private async Task DriveLoop(CancellationToken token)
        {
            int acked=0; bool profileSent=false;
            while(!token.IsCancellationRequested&&_device!=null&&_device.ConnectionStatus==BluetoothConnectionStatus.Connected)
            {
                try
                {
                    int controls=Volatile.Read(ref _controlCount); if(controls>acked){acked=controls;await WritePayload(ReplyPayload());}
                    double weight;int same;lock(this){weight=_currentWeight;same=_sameCount;}
                    if(weight>=5){await WritePayload(SyncPayload(weight));if(weight>=20&&same>=3&&!profileSent){await WritePayload(UserListPayload(weight));await WritePayload(new byte[]{0xBD,0x09});profileSent=true;RaiseStatus("Profile synchronized. Keep standing still while BIA completes…");}}
                    if(weight<2)profileSent=false; await Task.Delay(400,token);
                }
                catch(OperationCanceledException){return;}
                catch(Exception ex){if(_device==null||_device.ConnectionStatus!=BluetoothConnectionStatus.Connected)return;RaiseStatus("Bluetooth retry: "+ex.Message);Thread.Sleep(700);}
            }
        }

        private async Task WritePayload(byte[] payload)
        {
            if(_write==null)return; var frames=BuildFrames(payload); await _writeLock.WaitAsync();
            try { foreach(var frame in frames){using(var writer=new DataWriter()){writer.WriteBytes(frame);var buffer=writer.DetachBuffer();var status=await _write.WriteValueAsync(buffer,GattWriteOption.WriteWithResponse).AsTask();if(status!=GattCommunicationStatus.Success)await _write.WriteValueAsync(buffer,GattWriteOption.WriteWithoutResponse).AsTask();}} }
            finally{_writeLock.Release();}
        }
        private List<byte[]> BuildFrames(byte[] payload)
        {
            var list=new List<byte[]>();int offset=0,frag=0;do{var f=new byte[20];f[0]=(byte)_sequence;f[1]=(byte)payload.Length;f[2]=(byte)frag;int count=Math.Min(16,payload.Length-offset);Array.Copy(payload,offset,f,3,count);f[19]=Checksum(f);list.Add(f);offset+=16;frag++;}while(offset<payload.Length);_sequence=(_sequence+1)&255;return list;
        }
        private byte[] SyncPayload(double weight)
        {
            var p=new byte[16];p[0]=0xBA;uint unix=(uint)(DateTime.UtcNow-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds;PutU32(p,1,unix);p[5]=0;p[6]=0x78;p[11]=(byte)Math.Round(_profile.HeightCm);PutU16(p,12,WeightRaw(weight));p[14]=(byte)((_profile.Age&0x7F)|(_profile.Male?0x80:0));p[15]=(byte)(_profile.Athlete?0x0F:0x2F);return p;
        }
        private byte[] UserListPayload(double weight){var p=new byte[10];p[0]=0xBB;p[1]=1;p[6]=(byte)Math.Round(_profile.HeightCm);PutU16(p,7,WeightRaw(weight));p[9]=(byte)((_profile.Age&0x7F)|(_profile.Male?0x80:0));return p;}
        private byte[] ReplyPayload(){var p=new byte[]{(byte)0xB0,(byte)_reply,0};_reply=(_reply+1)&255;return p;}
        private static ushort WeightRaw(double kg){int r=((int)Math.Round(kg*100))&0x7FFF;if(r>0)r|=0x8000;return(ushort)r;}
        private static void PutU16(byte[] b,int o,ushort v){b[o]=(byte)(v>>8);b[o+1]=(byte)v;}
        private static void PutU32(byte[] b,int o,uint v){b[o]=(byte)(v>>24);b[o+1]=(byte)(v>>16);b[o+2]=(byte)(v>>8);b[o+3]=(byte)v;}
        private static int ReadU24(byte[] b,int o){return(b[o]<<16)|(b[o+1]<<8)|b[o+2];}
        private static byte Checksum(byte[] f){int s=0;for(int i=3;i<19;i++)s+=f[i];return(byte)(s&0x1F);}
        private static string FormatAddress(ulong a){var bytes=BitConverter.GetBytes(a);Array.Reverse(bytes);return string.Join(":",bytes.Skip(2).Select(x=>x.ToString("X2")));}
        private void RaiseStatus(string s){if(Status!=null)Status(s);}private void RaiseError(string s){if(Error!=null)Error(s);}private void RaiseDisconnected(){Interlocked.Exchange(ref _connecting,0);if(Disconnected!=null)Disconnected();}

        public void Disconnect()
        {
            if(_watcher!=null){try{_watcher.Stop();}catch{}} CleanupDevice();Interlocked.Exchange(ref _connecting,0);RaiseDisconnected();
        }
        private void CleanupDevice()
        {
            if(_driveCancel!=null){_driveCancel.Cancel();_driveCancel.Dispose();_driveCancel=null;}
            if(_notify!=null)_notify.ValueChanged-=OnValueChanged;if(_indicate!=null)_indicate.ValueChanged-=OnValueChanged;
            _write=_notify=_indicate=null;if(_service!=null){_service.Dispose();_service=null;}if(_session!=null){try{_session.MaintainConnection=false;}catch{} _session.Dispose();_session=null;}if(_device!=null){_device.ConnectionStatusChanged-=DeviceConnectionChanged;_device.Dispose();_device=null;}
        }
        public void Dispose(){Disconnect();_writeLock.Dispose();}
    }

    public sealed class ScaleApplication : IDisposable
    {
        private Window _window;
        private TextBox _height,_age;
        private ComboBox _sex;
        private CheckBox _athlete;
        private Button _connect,_disconnect,_overviewButton,_historyButton;
        private Ellipse _connectionDot;
        private TextBlock _connectionText,_weightText,_weightAssessment,_weightReference,_status;
        private Grid _weightBarHost,_historyPanel;
        private StackPanel _dashboard;
        private Border _empty;
        private UniformGrid _cards;
        private ScrollViewer _overview;
        private DataGrid _historyGrid;
        private readonly ScaleBluetooth _ble=new ScaleBluetooth();
        private ProfileSettings _settings;
        private UserProfile _profile;
        private List<HistoryRecord> _historyRows;
        private ScaleBar _weightBar;

        public Window CreateWindow()
        {
            var asm=Assembly.GetExecutingAssembly();using(var stream=asm.GetManifestResourceStream("MainWindow.xaml")){if(stream==null)throw new InvalidOperationException("Embedded UI resource is missing.");using(var reader=XmlReader.Create(stream))_window=(Window)XamlReader.Load(reader);}
            _height=Find<TextBox>("HeightBox");_age=Find<TextBox>("AgeBox");_sex=Find<ComboBox>("SexBox");_athlete=Find<CheckBox>("AthleteBox");
            _connect=Find<Button>("ConnectButton");_disconnect=Find<Button>("DisconnectButton");_overviewButton=Find<Button>("OverviewButton");_historyButton=Find<Button>("HistoryButton");
            _connectionDot=Find<Ellipse>("ConnectionDot");_connectionText=Find<TextBlock>("ConnectionText");_weightText=Find<TextBlock>("WeightText");
            _weightAssessment=Find<TextBlock>("WeightAssessment");_weightReference=Find<TextBlock>("WeightReference");_status=Find<TextBlock>("StatusText");
            _weightBarHost=Find<Grid>("WeightBarHost");_historyPanel=Find<Grid>("HistoryPanel");_dashboard=Find<StackPanel>("Dashboard");_empty=Find<Border>("EmptyPanel");
            _cards=Find<UniformGrid>("CardsPanel");_overview=Find<ScrollViewer>("OverviewScroll");_historyGrid=Find<DataGrid>("HistoryGrid");
            _weightBar=new ScaleBar();_weightBarHost.Children.Add(_weightBar);
            _settings=DataStore.LoadSettings();_historyRows=DataStore.LoadHistory();PopulateProfile();RefreshHistory();
            if(TryProfile(false)&&_historyRows.Count>0)RenderResult(_historyRows[0].ToResult(),false);
            WireEvents();return _window;
        }
        private T Find<T>(string name) where T:class {return _window.FindName(name) as T;}
        private void PopulateProfile(){_height.Text=_settings.height_cm??"";_age.Text=_settings.age??"";_sex.SelectedIndex=string.Equals(_settings.sex,"Female",StringComparison.OrdinalIgnoreCase)?1:0;_athlete.IsChecked=_settings.athlete;}
        private void WireEvents()
        {
            _connect.Click+=delegate{Connect();};_disconnect.Click+=delegate{_ble.Disconnect();};
            _overviewButton.Click+=delegate{ShowOverview();};_historyButton.Click+=delegate{ShowHistory();};
            Find<Button>("OpenLogsButton").Click+=delegate{string p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"FFB0 Scale Logs");Directory.CreateDirectory(p);Process.Start(new ProcessStartInfo{FileName=p,UseShellExecute=true});};
            Find<Button>("ExportButton").Click+=delegate{ExportCsv();};
            _window.Closed+=delegate{Dispose();};
            _ble.Status+=s=>Ui(delegate{_status.Text=s;});_ble.Connected+=a=>Ui(delegate{SetConnection(true,"Connected · "+a);});
            _ble.Disconnected+=()=>Ui(delegate{SetConnection(false,"Disconnected");});_ble.Weight+=(kg,stable)=>Ui(delegate{ShowDashboard();_weightText.Text=kg.ToString("F2")+" kg"+(stable?"  ✓":"");});
            _ble.Result+=r=>Ui(delegate{RenderResult(r,true);});_ble.Error+=s=>Ui(delegate{_status.Text=s;SetConnection(false,"Disconnected");MessageBox.Show(_window,s,"FFB0 Body Scale",MessageBoxButton.OK,MessageBoxImage.Warning);});
        }
        private void Ui(Action a){if(_window.Dispatcher.CheckAccess())a();else _window.Dispatcher.BeginInvoke(a);}
        private bool TryProfile(bool showError)
        {
            double h;int age;if(!double.TryParse(_height.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out h)||h<100||h>230){if(showError)MessageBox.Show(_window,"Height must be between 100 and 230 cm.");return false;}
            if(!int.TryParse(_age.Text,out age)||age<18||age>120){if(showError)MessageBox.Show(_window,"Age must be between 18 and 120.");return false;}
            _profile=new UserProfile{HeightCm=h,Age=age,Male=_sex.SelectedIndex!=1,Athlete=_athlete.IsChecked==true};return true;
        }
        private void Connect()
        {
            if(!TryProfile(true))return;_settings.height_cm=_height.Text;_settings.age=_age.Text;_settings.sex=_profile.Male?"Male":"Female";_settings.athlete=_profile.Athlete;DataStore.SaveSettings(_settings);
            _connect.IsEnabled=false;_disconnect.IsEnabled=true;_connectionDot.Fill=new SolidColorBrush(Color.FromRgb(223,148,31));_connectionText.Text="Connecting…";_ble.Connect(_profile);
        }
        private void SetConnection(bool connected,string text){_connect.IsEnabled=!connected;_disconnect.IsEnabled=connected;_connectionDot.Fill=new SolidColorBrush(connected?References.Good:Color.FromRgb(154,166,179));_connectionText.Text=text;}
        private void ShowOverview(){_overview.Visibility=Visibility.Visible;_historyPanel.Visibility=Visibility.Collapsed;_overviewButton.Background=new SolidColorBrush(Color.FromRgb(231,241,253));_overviewButton.Foreground=new SolidColorBrush(Color.FromRgb(30,103,180));_historyButton.Background=Brushes.Transparent;_historyButton.Foreground=new SolidColorBrush(Color.FromRgb(113,128,149));}
        private void ShowHistory(){_overview.Visibility=Visibility.Collapsed;_historyPanel.Visibility=Visibility.Visible;_historyButton.Background=new SolidColorBrush(Color.FromRgb(231,241,253));_historyButton.Foreground=new SolidColorBrush(Color.FromRgb(30,103,180));_overviewButton.Background=Brushes.Transparent;_overviewButton.Foreground=new SolidColorBrush(Color.FromRgb(113,128,149));}
        private void ShowDashboard(){_empty.Visibility=Visibility.Collapsed;_dashboard.Visibility=Visibility.Visible;}
        private Brush Tone(string tone){if(tone=="good")return new SolidColorBrush(References.Good);if(tone=="caution")return new SolidColorBrush(References.Caution);if(tone=="bad")return new SolidColorBrush(References.Bad);if(tone=="info")return new SolidColorBrush(References.Info);return new SolidColorBrush(Color.FromRgb(107,120,138));}
        private void RenderResult(MeasurementResult r,bool save)
        {
            if(_profile==null&&!TryProfile(false))return;ShowDashboard();ShowOverview();_weightText.Text=r.weight_kg.ToString("F2")+" kg";var wa=References.WeightAssessment(r,_profile);_weightAssessment.Text=wa.Label;_weightAssessment.Foreground=Tone(wa.Tone);_weightReference.Text=wa.Reference;_weightBar.Configure(r.weight_kg,References.WeightSpec(r,_profile));
            _status.Text=save?"Measurement complete — saved locally.":"Showing the latest locally saved measurement.";_cards.Children.Clear();
            AddCard("BMI","bmi",r.bmi,r.bmi.ToString("F1"),r);AddCard("Body fat","body_fat_percent",r.body_fat_percent,r.body_fat_percent.ToString("F1")+"%",r);AddCard("Body water","body_water_percent",r.body_water_percent,r.body_water_percent.ToString("F1")+"%",r);
            AddCard("Muscle","muscle_percent",r.muscle_percent,r.muscle_percent.ToString("F1")+"%",r);AddCard("Skeletal muscle","skeletal_muscle_percent",r.skeletal_muscle_percent,r.skeletal_muscle_percent.ToString("F1")+"%",r);AddCard("Protein","protein_percent",r.protein_percent,r.protein_percent.ToString("F1")+"%",r);
            AddCard("Visceral fat","visceral_fat",r.visceral_fat,r.visceral_fat.ToString("F1"),r);AddCard("Bone mass","bone_mass_kg",r.bone_mass_kg,r.bone_mass_kg.ToString("F1")+" kg",r);AddCard("BMR","bmr_kcal",r.bmr_kcal,r.bmr_kcal.ToString("F0")+" kcal",r);
            AddCard("Metabolic age","metabolic_age",r.metabolic_age,r.metabolic_age.ToString("F0"),r);AddCard("Body score","body_score",r.body_score,r.body_score.ToString("F1"),r);AddCard("Subcutaneous fat","subcutaneous_fat_percent",r.subcutaneous_fat_percent,r.subcutaneous_fat_percent.ToString("F1")+"%",r);
            AddCard("Heart rate","heart_rate_bpm",r.heart_rate_bpm,r.heart_rate_bpm.ToString("F0")+" bpm",r);AddCard("Impedance","impedance_ohm",r.impedance_ohm,r.impedance_ohm.ToString("F0")+" ohm",r);
            if(save){_historyRows.Insert(0,HistoryRecord.FromResult(r));if(_historyRows.Count>1000)_historyRows.RemoveRange(1000,_historyRows.Count-1000);DataStore.SaveHistory(_historyRows);RefreshHistory();}
        }
        private void AddCard(string label,string field,double value,string display,MeasurementResult r)
        {
            var a=References.Assess(field,value,r,_profile);var outer=new Border{Background=Brushes.White,BorderBrush=new SolidColorBrush(Color.FromRgb(226,232,240)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(16,14,16,12),Margin=new Thickness(5)};
            var stack=new StackPanel();outer.Child=stack;stack.Children.Add(new TextBlock{Text=label.ToUpperInvariant(),Foreground=new SolidColorBrush(Color.FromRgb(119,133,151)),FontSize=11,FontWeight=FontWeights.SemiBold});
            stack.Children.Add(new TextBlock{Text=display,Foreground=new SolidColorBrush(Color.FromRgb(20,32,51)),FontSize=27,FontWeight=FontWeights.Bold,Margin=new Thickness(0,6,0,1)});
            stack.Children.Add(new TextBlock{Text=a.Label,Foreground=Tone(a.Tone),FontSize=12,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,2,0,0)});stack.Children.Add(new TextBlock{Text=a.Reference,Foreground=new SolidColorBrush(Color.FromRgb(107,120,138)),FontSize=10,Margin=new Thickness(0,3,0,0)});
            var bar=new ScaleBar{Margin=new Thickness(0,9,0,0)};bar.Configure(value,References.Spec(field,value,r,_profile));stack.Children.Add(bar);_cards.Children.Add(outer);
        }
        private void RefreshHistory(){_historyGrid.ItemsSource=null;_historyGrid.ItemsSource=_historyRows;}
        private void ExportCsv()
        {
            var d=new SaveFileDialog{Title="Export measurement history",Filter="CSV files (*.csv)|*.csv",FileName="FFB0-scale-history.csv"};if(d.ShowDialog(_window)!=true)return;
            using(var w=new StreamWriter(d.FileName,false,new UTF8Encoding(true))){w.WriteLine("measured_at,weight_kg,bmi,body_fat_percent,body_water_percent,muscle_percent,heart_rate_bpm,impedance_ohm");foreach(var r in _historyRows)w.WriteLine(string.Join(",",new[]{r.measured_at,r.weight_kg.ToString(CultureInfo.InvariantCulture),r.bmi.ToString(CultureInfo.InvariantCulture),r.body_fat_percent.ToString(CultureInfo.InvariantCulture),r.body_water_percent.ToString(CultureInfo.InvariantCulture),r.muscle_percent.ToString(CultureInfo.InvariantCulture),r.heart_rate_bpm.ToString(CultureInfo.InvariantCulture),r.impedance_ohm.ToString(CultureInfo.InvariantCulture)}));}
            _status.Text="History exported to "+d.FileName;
        }
        public void Dispose(){_ble.Dispose();}
    }

    public sealed class ClinicApplication : IDisposable
    {
        Window w; TextBox name,height,age,search; ComboBox sex; CheckBox athlete; Button connect,disconnect,openRecord,printButton;
        Ellipse dot; TextBlock connection,pageTitle,pageSubtitle,status,resultStatus,weight,weightAssessment,weightReference,count,detailPatient,detailMeta,searchHint,activityTitle,liveWeight;
        Grid archivePanel,weightHost; ScrollViewer measurementPanel; Border patientForm,detailHeader,empty,connectionPill; StackPanel dashboard; UniformGrid cards; DataGrid grid;
        Popup suggestionsPopup; ListBox suggestionsList; ProgressBar activityProgress; bool applyingSuggestion;
        MeasurementResult currentResult; HistoryRecord currentRecord;
        ScaleBar weightBar; readonly ScaleBluetooth ble=new ScaleBluetooth(); List<HistoryRecord> rows; UserProfile profile;
        PrintManager printManager; WpfPrintDocumentSource printSource; bool printRegistered; string nativePrintProbePath;

        public Window CreateWindow()
        {
            using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml")) using(var xr=XmlReader.Create(s)) w=(Window)XamlReader.Load(xr);
            name=F<TextBox>("PatientNameBox");height=F<TextBox>("HeightBox");age=F<TextBox>("AgeBox");search=F<TextBox>("SearchBox");sex=F<ComboBox>("SexBox");athlete=F<CheckBox>("AthleteBox");
            connect=F<Button>("ConnectButton");disconnect=F<Button>("DisconnectButton");openRecord=F<Button>("OpenRecordButton");printButton=F<Button>("PrintButton");dot=F<Ellipse>("ConnectionDot");connection=F<TextBlock>("ConnectionText");pageTitle=F<TextBlock>("PageTitle");pageSubtitle=F<TextBlock>("PageSubtitle");
            status=F<TextBlock>("StatusText");activityTitle=F<TextBlock>("ActivityTitle");liveWeight=F<TextBlock>("LiveWeightText");activityProgress=F<ProgressBar>("ActivityProgress");resultStatus=F<TextBlock>("ResultStatusText");weight=F<TextBlock>("WeightText");weightAssessment=F<TextBlock>("WeightAssessment");weightReference=F<TextBlock>("WeightReference");count=F<TextBlock>("RecordCountText");
            detailPatient=F<TextBlock>("DetailPatientText");detailMeta=F<TextBlock>("DetailMetaText");searchHint=F<TextBlock>("SearchHint");archivePanel=F<Grid>("ArchivePanel");measurementPanel=F<ScrollViewer>("MeasurementScroll");weightHost=F<Grid>("WeightBarHost");
            patientForm=F<Border>("PatientFormCard");detailHeader=F<Border>("DetailHeader");empty=F<Border>("EmptyPanel");connectionPill=F<Border>("ConnectionPill");suggestionsPopup=F<Popup>("PatientSuggestionsPopup");suggestionsList=F<ListBox>("PatientSuggestionsList");dashboard=F<StackPanel>("Dashboard");cards=F<UniformGrid>("CardsPanel");grid=F<DataGrid>("RecordsGrid");weightBar=new ScaleBar();weightHost.Children.Add(weightBar);
            sex.SelectedIndex=0; rows=DataStore.LoadHistory().OrderByDescending(x=>x.measured_at).ToList(); Wire();w.Loaded+=RegisterNativePrinting;Refresh(); ShowArchive(); return w;
        }
        T F<T>(string n) where T:class{return w.FindName(n) as T;}
        void Wire()
        {
            F<Button>("ArchiveNavButton").Click+=(a,b)=>ShowArchive();F<Button>("NewNavButton").Click+=(a,b)=>ShowNew();F<Button>("BackButton").Click+=(a,b)=>ShowArchive();
            openRecord.Click+=(a,b)=>OpenSelected();grid.MouseDoubleClick+=(a,b)=>OpenSelected();grid.SelectionChanged+=(a,b)=>openRecord.IsEnabled=grid.SelectedItem!=null;search.TextChanged+=(a,b)=>{searchHint.Visibility=string.IsNullOrEmpty(search.Text)?Visibility.Visible:Visibility.Collapsed;Refresh();};connect.Click+=(a,b)=>Connect();disconnect.Click+=(a,b)=>ble.Disconnect();
            printButton.Click+=async (a,b)=>await ShowNativePrintDialog();
            name.TextChanged+=(a,b)=>{ValidateInputs();UpdatePatientSuggestions();};height.TextChanged+=(a,b)=>ValidateInputs();age.TextChanged+=(a,b)=>ValidateInputs();sex.SelectionChanged+=(a,b)=>ValidateInputs();
            name.KeyDown+=NameKeyDown;suggestionsList.KeyDown+=SuggestionKeyDown;suggestionsList.MouseLeftButtonUp+=(a,b)=>ApplySelectedSuggestion();
            F<Button>("OpenLogsButton").Click+=(a,b)=>{Directory.CreateDirectory(DataStore.LocalDir);Process.Start(new ProcessStartInfo{FileName=DataStore.LocalDir,UseShellExecute=true});};F<Button>("ExportButton").Click+=(a,b)=>Export();w.Closed+=(a,b)=>Dispose();
            ble.Status+=s=>UI(()=>{status.Text=RomanianStatus(s);if(s.IndexOf("Profile synchronized",StringComparison.OrdinalIgnoreCase)>=0)activityTitle.Text="Se analizează compoziția corporală…";});
            ble.Connected+=a=>UI(()=>{SetConnection(true,"Conectat · "+a);SetActivity("Măsurare în curs","Urcați desculț pe cântar și rămâneți nemișcat(ă).",true,null);});
            ble.Disconnected+=()=>UI(()=>{SetConnection(false,"Deconectat");if(measurementPanel.Visibility==Visibility.Visible&&dashboard.Visibility!=Visibility.Visible)SetActivity("Cântarul este deconectat","Verificați dacă este pornit, apoi încercați din nou.",false,null);});
            ble.Weight+=(kg,stable)=>UI(()=>SetActivity(stable?"Greutate stabilă — se calculează BIA…":"Măsurare în curs",stable?"Rămâneți nemișcat(ă) până la finalizarea analizei.":"Greutatea a fost detectată. Rămâneți pe cântar.",true,kg));
            ble.Result+=r=>UI(()=>Render(r,true,null));ble.Error+=s=>UI(()=>{SetActivity("Măsurarea nu a putut fi finalizată",RomanianStatus(s),false,null);SetConnection(false,"Deconectat");MessageBox.Show(w,RomanianStatus(s),"Cântar FFB0",MessageBoxButton.OK,MessageBoxImage.Warning);});
        }
        void UI(Action a){if(w.Dispatcher.CheckAccess())a();else w.Dispatcher.BeginInvoke(a);}
        void ShowArchive(){if(measurementPanel.Visibility==Visibility.Visible)ble.Disconnect();archivePanel.Visibility=Visibility.Visible;measurementPanel.Visibility=Visibility.Collapsed;connectionPill.Visibility=Visibility.Collapsed;pageTitle.Text="Arhiva pacienților";pageSubtitle.Text="Măsurători organizate pe pacient și dată";Refresh();}
        void ShowNew(){ble.Disconnect();archivePanel.Visibility=Visibility.Collapsed;measurementPanel.Visibility=Visibility.Visible;connectionPill.Visibility=Visibility.Visible;patientForm.Visibility=Visibility.Visible;detailHeader.Visibility=Visibility.Collapsed;empty.Visibility=Visibility.Visible;dashboard.Visibility=Visibility.Collapsed;pageTitle.Text="Măsurare nouă";pageSubtitle.Text="Înregistrați pacientul, apoi conectați un cântar compatibil cu protocolul FFB0";applyingSuggestion=true;name.Text="";height.Text="";age.Text="";sex.SelectedIndex=0;athlete.IsChecked=false;applyingSuggestion=false;suggestionsPopup.IsOpen=false;SetActivity("Pregătit pentru măsurare","Completați datele pacientului pentru a activa conectarea.",false,null);SetConnection(false,"Deconectat");ValidateInputs();}
        void OpenSelected(){var r=grid.SelectedItem as HistoryRecord;if(r==null)return;ble.Disconnect();profile=new UserProfile{HeightCm=r.height_cm,Age=r.age,Male=r.sex!="Feminin",Athlete=r.athlete};archivePanel.Visibility=Visibility.Collapsed;measurementPanel.Visibility=Visibility.Visible;connectionPill.Visibility=Visibility.Collapsed;patientForm.Visibility=Visibility.Collapsed;detailHeader.Visibility=Visibility.Visible;pageTitle.Text="Rezultatul măsurătorii";pageSubtitle.Text="Fișă salvată în arhiva locală";detailPatient.Text=string.IsNullOrWhiteSpace(r.patient_name)?"Pacient fără nume":r.patient_name;detailMeta.Text=r.DateDisplay+"  ·  "+r.height_cm.ToString("F0")+" cm  ·  "+r.age+" ani  ·  "+r.sex;Render(r.ToResult(),false,r);}
        void ValidateInputs(){if(connect==null)return;double h;int a;connect.IsEnabled=!string.IsNullOrWhiteSpace(name.Text)&&double.TryParse(height.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out h)&&h>=100&&h<=230&&int.TryParse(age.Text,out a)&&a>=18&&a<=120&&sex.SelectedIndex>=0;}
        void SetActivity(string title,string message,bool busy,double? kg)
        {
            activityTitle.Text=title;status.Text=message;activityProgress.Visibility=busy?Visibility.Visible:Visibility.Collapsed;
            if(kg.HasValue){liveWeight.Text=kg.Value.ToString("F2")+" kg";liveWeight.Visibility=Visibility.Visible;}else{liveWeight.Text="";liveWeight.Visibility=Visibility.Collapsed;}
        }
        void UpdatePatientSuggestions()
        {
            if(applyingSuggestion||suggestionsPopup==null)return;string q=(name.Text??"").Trim();
            if(q.Length==0){suggestionsPopup.IsOpen=false;return;}
            var matches=rows.Where(x=>!string.IsNullOrWhiteSpace(x.patient_name)&&x.patient_name.IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0)
                .GroupBy(x=>x.patient_name,StringComparer.CurrentCultureIgnoreCase).Select(g=>g.OrderByDescending(x=>x.measured_at).First())
                .OrderBy(x=>x.patient_name.StartsWith(q,StringComparison.CurrentCultureIgnoreCase)?0:1).ThenByDescending(x=>x.measured_at).Take(6)
                .Select(x=>new PatientSuggestion{Name=x.patient_name,Detail=x.DateDisplay+"  ·  "+x.height_cm.ToString("F0")+" cm  ·  "+x.age+" ani",Record=x}).ToList();
            if(matches.Count==1&&string.Equals(matches[0].Name,q,StringComparison.CurrentCultureIgnoreCase)){suggestionsPopup.IsOpen=false;return;}
            suggestionsList.ItemsSource=matches;suggestionsList.SelectedIndex=-1;suggestionsPopup.Width=Math.Max(360,name.ActualWidth);suggestionsPopup.IsOpen=matches.Count>0;
        }
        void ApplySelectedSuggestion()
        {
            var s=suggestionsList.SelectedItem as PatientSuggestion;if(s==null)return;var r=s.Record;applyingSuggestion=true;
            name.Text=s.Name;height.Text=r.height_cm.ToString("F0");age.Text=r.age.ToString();sex.SelectedIndex=r.sex=="Feminin"?1:0;athlete.IsChecked=r.athlete;name.CaretIndex=name.Text.Length;
            applyingSuggestion=false;suggestionsPopup.IsOpen=false;SetActivity("Pacient existent selectat","Datele ultimei măsurători au fost completate. Verificați-le înainte de conectare.",false,null);ValidateInputs();
        }
        void NameKeyDown(object sender,System.Windows.Input.KeyEventArgs e)
        {
            if(!suggestionsPopup.IsOpen)return;if(e.Key==System.Windows.Input.Key.Down){suggestionsList.SelectedIndex=Math.Min(suggestionsList.Items.Count-1,suggestionsList.SelectedIndex+1);e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.Up){suggestionsList.SelectedIndex=Math.Max(0,suggestionsList.SelectedIndex-1);e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.Enter){if(suggestionsList.SelectedIndex<0&&suggestionsList.Items.Count>0)suggestionsList.SelectedIndex=0;ApplySelectedSuggestion();e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.Escape){suggestionsPopup.IsOpen=false;e.Handled=true;}
        }
        void SuggestionKeyDown(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Enter){ApplySelectedSuggestion();e.Handled=true;}else if(e.Key==System.Windows.Input.Key.Escape){suggestionsPopup.IsOpen=false;name.Focus();e.Handled=true;}}
        bool ReadProfile(bool warn)
        {
            double h;int a;if(string.IsNullOrWhiteSpace(name.Text)){if(warn)MessageBox.Show(w,"Introduceți numele complet al pacientului.","Date incomplete");return false;}
            if(!double.TryParse(height.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out h)||h<100||h>230){if(warn)MessageBox.Show(w,"Înălțimea trebuie să fie între 100 și 230 cm.","Date incorecte");return false;}
            if(!int.TryParse(age.Text,out a)||a<18||a>120){if(warn)MessageBox.Show(w,"Vârsta trebuie să fie între 18 și 120 de ani.","Date incorecte");return false;}
            profile=new UserProfile{HeightCm=h,Age=a,Male=sex.SelectedIndex!=1,Athlete=athlete.IsChecked==true};return true;
        }
        void Connect(){if(!ReadProfile(true))return;suggestionsPopup.IsOpen=false;connect.IsEnabled=false;disconnect.IsEnabled=true;disconnect.Visibility=Visibility.Visible;disconnect.Content="Anulează";dot.Fill=new SolidColorBrush(References.Caution);connection.Text="Se conectează…";SetActivity("Se caută și se conectează cântarul…","Mențineți cântarul activ și închideți Fitdays+ pe telefoanele din apropiere.",true,null);ble.Connect(profile);}
        void SetConnection(bool on,string text){connect.IsEnabled=!on;disconnect.IsEnabled=on;disconnect.Visibility=on?Visibility.Visible:Visibility.Collapsed;disconnect.Content="Deconectare";dot.Fill=new SolidColorBrush(on?References.Good:Color.FromRgb(154,166,179));connection.Text=text;if(!on)ValidateInputs();}
        void ShowDashboard(){empty.Visibility=Visibility.Collapsed;dashboard.Visibility=Visibility.Visible;}
        Brush Tone(string t){return new SolidColorBrush(t=="good"?References.Good:t=="caution"?References.Caution:t=="bad"?References.Bad:t=="info"?References.Info:References.Gray);}
        string L(string s)
        {
            var d=new Dictionary<string,string>{{"Underweight","Subponderal"},{"Healthy weight","Greutate normală"},{"Overweight","Supraponderal"},{"Obesity","Obezitate"},{"Severe obesity","Obezitate severă"},{"Low","Scăzut"},{"Healthy","Normal"},{"Elevated","Crescut"},{"High","Ridicat"},{"Normal","Normal"},{"Very high","Foarte ridicat"},{"Below reference","Sub interval"},{"Within reference","În interval"},{"Above reference","Peste interval"},{"Estimated baseline","Estimare de bază"},{"No universal healthy band","Fără interval universal"},{"Younger than profile age","Mai mică decât vârsta pacientului"},{"Matches profile age","Egală cu vârsta pacientului"},{"Older than profile age","Mai mare decât vârsta pacientului"},{"Needs attention","Necesită atenție"},{"Fair","Satisfăcător"},{"Good","Bun"},{"Excellent","Excelent"},{"Below resting range","Sub intervalul de repaus"},{"Within resting range","În intervalul de repaus"},{"Above resting range","Peste intervalul de repaus"},{"Raw sensor reading","Valoare brută a senzorului"},{"Individual; no health category","Valoare individuală; fără categorie clinică"}};
            string x;return d.TryGetValue(s,out x)?x:s.Replace("Healthy:","Interval sănătos:").Replace("Reference: about","Referință: aproximativ").Replace("Reference:","Referință:").Replace("Profile age:","Vârsta pacientului:").Replace("Typical resting:","Repaus uzual:").Replace("App-style score:","Scor:").Replace("Healthy rating:","Interval sănătos:");
        }
        void Render(MeasurementResult r,bool save,HistoryRecord source)
        {
            if(profile==null&&!ReadProfile(false))return;currentResult=r;currentRecord=source;ShowDashboard();weight.Text=r.weight_kg.ToString("F2")+" kg";var wa=References.WeightAssessment(r,profile);weightAssessment.Text=L(wa.Label);weightAssessment.Foreground=Tone(wa.Tone);weightReference.Text="Interval de greutate normală: "+(18.5*Math.Pow(profile.HeightCm/100,2)).ToString("F1")+"–"+(23.9*Math.Pow(profile.HeightCm/100,2)).ToString("F1")+" kg";weightBar.Configure(r.weight_kg,References.WeightSpec(r,profile));resultStatus.Text=save?"Măsurare finalizată și salvată local.":"Măsurare salvată · estimări orientative, fără valoare de diagnostic.";cards.Children.Clear();
            Add("IMC","bmi",r.bmi,r.bmi.ToString("F1"),r);Add("Grăsime corporală","body_fat_percent",r.body_fat_percent,r.body_fat_percent.ToString("F1")+"%",r);Add("Apă corporală","body_water_percent",r.body_water_percent,r.body_water_percent.ToString("F1")+"%",r);Add("Masă musculară","muscle_percent",r.muscle_percent,r.muscle_percent.ToString("F1")+"%",r);Add("Mușchi scheletici","skeletal_muscle_percent",r.skeletal_muscle_percent,r.skeletal_muscle_percent.ToString("F1")+"%",r);Add("Proteine","protein_percent",r.protein_percent,r.protein_percent.ToString("F1")+"%",r);Add("Grăsime viscerală","visceral_fat",r.visceral_fat,r.visceral_fat.ToString("F1"),r);Add("Masă osoasă","bone_mass_kg",r.bone_mass_kg,r.bone_mass_kg.ToString("F1")+" kg",r);Add("Metabolism bazal","bmr_kcal",r.bmr_kcal,r.bmr_kcal.ToString("F0")+" kcal",r);Add("Vârstă metabolică","metabolic_age",r.metabolic_age,r.metabolic_age.ToString("F0"),r);Add("Scor corporal","body_score",r.body_score,r.body_score.ToString("F1"),r);Add("Grăsime subcutanată","subcutaneous_fat_percent",r.subcutaneous_fat_percent,r.subcutaneous_fat_percent.ToString("F1")+"%",r);Add("Puls","heart_rate_bpm",r.heart_rate_bpm,r.heart_rate_bpm.ToString("F0")+" bpm",r);Add("Impedanță","impedance_ohm",r.impedance_ohm,r.impedance_ohm.ToString("F0")+" Ω",r);
            if(save){var rec=HistoryRecord.FromResult(r,name.Text.Trim(),profile);currentRecord=rec;rows.Insert(0,rec);if(rows.Count>5000)rows.RemoveRange(5000,rows.Count-5000);DataStore.SaveHistory(rows);patientForm.Visibility=Visibility.Collapsed;detailHeader.Visibility=Visibility.Visible;connectionPill.Visibility=Visibility.Collapsed;pageTitle.Text="Rezultatul măsurătorii";pageSubtitle.Text="Măsurare finalizată și salvată în arhivă";detailPatient.Text=rec.patient_name;detailMeta.Text=rec.DateDisplay+"  ·  "+rec.height_cm.ToString("F0")+" cm  ·  "+rec.age+" ani  ·  "+rec.sex;ble.Disconnect();Refresh();}
        }
        void RegisterNativePrinting(object sender,RoutedEventArgs e)
        {
            if(printRegistered)return;
            try
            {
                printSource=new WpfPrintDocumentSource
                {
                    Dispatcher=w.Dispatcher,
                    OnPaginatorRequired=(options,details)=>
                    {
                        var page=options.GetPageDescription(0);
                        if(!string.IsNullOrWhiteSpace(nativePrintProbePath))File.WriteAllText(nativePrintProbePath,"PREVIEW_RENDERED",new UTF8Encoding(false));
                        return BuildPrintDocument(page.PageSize.Width,page.PageSize.Height);
                    }
                };
                printManager=PrintManagerInterop.GetForWindow(new WindowInteropHelper(w).Handle);
                printManager.PrintTaskRequested+=PrintTaskRequested;
                printRegistered=true;
            }
            catch(Exception ex)
            {
                printRegistered=false;
                MessageBox.Show(w,"Serviciul modern de tipărire nu a putut fi inițializat: "+ex.Message,"Cântar FFB0",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
        void PrintTaskRequested(PrintManager sender,PrintTaskRequestedEventArgs args)
        {
            string patient=currentRecord!=null&&!string.IsNullOrWhiteSpace(currentRecord.patient_name)?currentRecord.patient_name:name.Text.Trim();
            var task=args.Request.CreatePrintTask("Raport compoziție corporală - "+patient,request=>request.SetSource(printSource));
            task.Options.Orientation=PrintOrientation.Portrait;
            task.Options.MediaSize=PrintMediaSize.IsoA4;
            var details=PrintTaskOptionDetails.GetFromPrintTaskOptions(task.Options);
            details.OptionChanged+=(a,b)=>printSource.InvalidatePreview();
        }
        async Task ShowNativePrintDialog()
        {
            if(currentResult==null||profile==null){MessageBox.Show(w,"Nu există un rezultat disponibil pentru tipărire.","Cântar FFB0");return;}
            try
            {
                if(!printRegistered)RegisterNativePrinting(w,new RoutedEventArgs());
                if(!printRegistered)return;
                await PrintManagerInterop.ShowPrintUIForWindowAsync(new WindowInteropHelper(w).Handle);
            }
            catch(Exception ex){MessageBox.Show(w,"Dialogul Microsoft Print nu a putut fi deschis: "+ex.Message,"Cântar FFB0",MessageBoxButton.OK,MessageBoxImage.Error);}
        }
        public void StartNativePrintProbe(string markerPath)
        {
            nativePrintProbePath=markerPath;PrepareReportTestData();
            w.ContentRendered+=async (a,b)=>await ShowNativePrintDialog();
        }
        FlowDocument BuildPrintDocument(double pageWidth,double pageHeight)
        {
            var r=currentResult;var rec=currentRecord;string patient=rec!=null&&!string.IsNullOrWhiteSpace(rec.patient_name)?rec.patient_name:name.Text.Trim();string date=rec!=null?rec.DateDisplay:DateTime.Now.ToString("dd.MM.yyyy, HH:mm",new CultureInfo("ro-RO"));
            var doc=new FlowDocument{FontFamily=new FontFamily("Segoe UI"),FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(26,40,58)),Background=Brushes.White,PageWidth=Math.Max(pageWidth,650),PageHeight=Math.Max(pageHeight,900),PagePadding=new Thickness(34),ColumnWidth=double.PositiveInfinity,ColumnGap=0};
            var head=new Grid{Margin=new Thickness(0,0,0,13)};head.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});head.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var brand=new StackPanel{VerticalAlignment=VerticalAlignment.Bottom};brand.Children.Add(new TextBlock{Text="FFB0 BODY SCALE",FontSize=20,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(19,34,56))});brand.Children.Add(new TextBlock{Text="Protocol FFB0 · raport local de compoziție corporală",FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(104,120,141)),Margin=new Thickness(0,3,0,0)});head.Children.Add(brand);var reportInfo=new StackPanel{HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom};reportInfo.Children.Add(new TextBlock{Text="RAPORT DE COMPOZIȚIE CORPORALĂ",FontSize=12,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(39,113,202))});reportInfo.Children.Add(new TextBlock{Text="Generat: "+DateTime.Now.ToString("dd.MM.yyyy, HH:mm"),FontSize=9,Foreground=new SolidColorBrush(Color.FromRgb(105,121,140)),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,3,0,0)});Grid.SetColumn(reportInfo,1);head.Children.Add(reportInfo);doc.Blocks.Add(new BlockUIContainer(head));
            var patientBox=new Border{Background=new SolidColorBrush(Color.FromRgb(239,246,253)),BorderBrush=new SolidColorBrush(Color.FromRgb(205,224,244)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(14),Margin=new Thickness(0,0,0,12)};var patientStack=new StackPanel();patientBox.Child=patientStack;patientStack.Children.Add(new TextBlock{Text=patient,FontSize=17,FontWeight=FontWeights.Bold});patientStack.Children.Add(new TextBlock{Text=date+"  ·  "+profile.HeightCm.ToString("F0")+" cm  ·  "+profile.Age+" ani  ·  "+(profile.Male?"Masculin":"Feminin")+(profile.Athlete?"  ·  Mod sportiv":""),FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(94,112,132)),Margin=new Thickness(0,4,0,0)});doc.Blocks.Add(new BlockUIContainer(patientBox));
            var wa=References.WeightAssessment(r,profile);var weightBox=new Border{BorderBrush=new SolidColorBrush(Color.FromRgb(220,229,239)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(14),Margin=new Thickness(0,0,0,11)};var wg=new Grid();weightBox.Child=wg;wg.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)});wg.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});var ws=new StackPanel();ws.Children.Add(new TextBlock{Text="GREUTATE",FontSize=9,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(116,133,152))});ws.Children.Add(new TextBlock{Text=r.weight_kg.ToString("F2")+" kg",FontSize=28,FontWeight=FontWeights.Bold});ws.Children.Add(new TextBlock{Text=L(wa.Label),FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=Tone(wa.Tone)});ws.Children.Add(new TextBlock{Text="Interval normal: "+(18.5*Math.Pow(profile.HeightCm/100,2)).ToString("F1")+"–"+(23.9*Math.Pow(profile.HeightCm/100,2)).ToString("F1")+" kg",FontSize=9,Foreground=new SolidColorBrush(Color.FromRgb(104,120,139)),Margin=new Thickness(0,3,0,0)});wg.Children.Add(ws);var wbar=new ScaleBar{Width=330,Margin=new Thickness(12,25,0,0)};wbar.Configure(r.weight_kg,References.WeightSpec(r,profile));Grid.SetColumn(wbar,1);wg.Children.Add(wbar);doc.Blocks.Add(new BlockUIContainer(weightBox));
            string[] labels={"IMC","Grăsime corporală","Apă corporală","Masă musculară","Mușchi scheletici","Proteine","Grăsime viscerală","Masă osoasă","Metabolism bazal","Vârstă metabolică","Scor corporal","Grăsime subcutanată","Puls","Impedanță"};
            string[] fields={"bmi","body_fat_percent","body_water_percent","muscle_percent","skeletal_muscle_percent","protein_percent","visceral_fat","bone_mass_kg","bmr_kcal","metabolic_age","body_score","subcutaneous_fat_percent","heart_rate_bpm","impedance_ohm"};
            double[] values={r.bmi,r.body_fat_percent,r.body_water_percent,r.muscle_percent,r.skeletal_muscle_percent,r.protein_percent,r.visceral_fat,r.bone_mass_kg,r.bmr_kcal,r.metabolic_age,r.body_score,r.subcutaneous_fat_percent,r.heart_rate_bpm,r.impedance_ohm};
            string[] displays={r.bmi.ToString("F1"),r.body_fat_percent.ToString("F1")+"%",r.body_water_percent.ToString("F1")+"%",r.muscle_percent.ToString("F1")+"%",r.skeletal_muscle_percent.ToString("F1")+"%",r.protein_percent.ToString("F1")+"%",r.visceral_fat.ToString("F1"),r.bone_mass_kg.ToString("F1")+" kg",r.bmr_kcal.ToString("F0")+" kcal",r.metabolic_age.ToString("F0"),r.body_score.ToString("F1"),r.subcutaneous_fat_percent.ToString("F1")+"%",r.heart_rate_bpm.ToString("F0")+" bpm",r.impedance_ohm.ToString("F0")+" Ω"};
            var table=new Table{CellSpacing=6};table.Columns.Add(new TableColumn{Width=new GridLength(1,GridUnitType.Star)});table.Columns.Add(new TableColumn{Width=new GridLength(1,GridUnitType.Star)});table.Columns.Add(new TableColumn{Width=new GridLength(1,GridUnitType.Star)});var group=new TableRowGroup();table.RowGroups.Add(group);for(int i=0;i<labels.Length;i+=3){var row=new TableRow();for(int j=0;j<3&&i+j<labels.Length;j++)row.Cells.Add(PrintMetricCell(labels[i+j],fields[i+j],values[i+j],displays[i+j],r));group.Rows.Add(row);}doc.Blocks.Add(table);
            var footer=new Paragraph(new Run("Estimările de compoziție corporală sunt orientative și nu constituie diagnostic medical. Rezultatele trebuie interpretate în context clinic.")){FontSize=8,Foreground=new SolidColorBrush(Color.FromRgb(103,118,136)),Margin=new Thickness(0,10,0,0),BorderBrush=new SolidColorBrush(Color.FromRgb(221,229,238)),BorderThickness=new Thickness(0,1,0,0),Padding=new Thickness(0,8,0,0)};doc.Blocks.Add(footer);return doc;
        }
        TableCell PrintMetricCell(string label,string field,double value,string display,MeasurementResult r)
        {
            var a=References.Assess(field,value,r,profile);var border=new Border{BorderBrush=new SolidColorBrush(Color.FromRgb(222,230,239)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Padding=new Thickness(9),Margin=new Thickness(1)};var stack=new StackPanel();border.Child=stack;stack.Children.Add(new TextBlock{Text=label.ToUpperInvariant(),FontSize=7.5,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(112,130,149))});stack.Children.Add(new TextBlock{Text=display,FontSize=17,FontWeight=FontWeights.Bold,Margin=new Thickness(0,2,0,0)});stack.Children.Add(new TextBlock{Text=L(a.Label)+"  ·  "+L(a.Reference),FontSize=7.5,Foreground=Tone(a.Tone),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,2,0,0)});var bar=new ScaleBar{Width=165,Margin=new Thickness(0,4,0,0)};bar.Configure(value,References.Spec(field,value,r,profile));stack.Children.Add(bar);var cell=new TableCell();cell.Blocks.Add(new BlockUIContainer(border));return cell;
        }
        void PrepareReportTestData()
        {
            profile=new UserProfile{HeightCm=178,Age=42,Male=true,Athlete=false};currentResult=BodyMath.Compute(73.4,515,74,profile);currentRecord=HistoryRecord.FromResult(currentResult,"Pacient demonstrativ",profile);
        }
        public bool ReportSelfTest()
        {
            PrepareReportTestData();var doc=BuildPrintDocument(793,1122);var paginator=((IDocumentPaginatorSource)doc).DocumentPaginator;paginator.PageSize=new Size(793,1122);paginator.ComputePageCount();return paginator.PageCount>0;
        }
        public void RenderReportPreview(string path)
        {
            PrepareReportTestData();var doc=BuildPrintDocument(793,1122);var paginator=((IDocumentPaginatorSource)doc).DocumentPaginator;paginator.PageSize=new Size(793,1122);paginator.ComputePageCount();var page=paginator.GetPage(0);var bitmap=new RenderTargetBitmap(793,1122,96,96,PixelFormats.Pbgra32);bitmap.Render(page.Visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);
        }
        void Add(string label,string field,double value,string display,MeasurementResult r){var a=References.Assess(field,value,r,profile);var b=new Border{Background=Brushes.White,BorderBrush=new SolidColorBrush(Color.FromRgb(223,231,240)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(16,14,16,12),Margin=new Thickness(5)};var s=new StackPanel();b.Child=s;s.Children.Add(new TextBlock{Text=label.ToUpperInvariant(),Foreground=new SolidColorBrush(Color.FromRgb(116,132,151)),FontSize=11,FontWeight=FontWeights.SemiBold});s.Children.Add(new TextBlock{Text=display,Foreground=new SolidColorBrush(Color.FromRgb(18,32,51)),FontSize=26,FontWeight=FontWeights.Bold,Margin=new Thickness(0,5,0,1)});s.Children.Add(new TextBlock{Text=L(a.Label),Foreground=Tone(a.Tone),FontSize=12,FontWeight=FontWeights.SemiBold});s.Children.Add(new TextBlock{Text=L(a.Reference),Foreground=new SolidColorBrush(Color.FromRgb(104,119,139)),FontSize=10,Margin=new Thickness(0,3,0,0)});var bar=new ScaleBar{Margin=new Thickness(0,8,0,0)};bar.Configure(value,References.Spec(field,value,r,profile));s.Children.Add(bar);cards.Children.Add(b);}
        void Refresh(){string q=(search==null?"":search.Text).Trim();var filtered=rows.Where(r=>string.IsNullOrEmpty(q)||(r.patient_name??"").IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0||r.DateDisplay.IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0).ToList();grid.ItemsSource=null;grid.ItemsSource=filtered;F<StackPanel>("ArchiveEmpty").Visibility=filtered.Count==0?Visibility.Visible:Visibility.Collapsed;count.Text=filtered.Count+" "+(filtered.Count==1?"măsurătoare":"măsurători");}
        void Export(){var d=new SaveFileDialog{Title="Exportă arhiva",Filter="Fișiere CSV (*.csv)|*.csv",FileName="arhiva-cantar-clinica.csv"};if(d.ShowDialog(w)!=true)return;using(var sw=new StreamWriter(d.FileName,false,new UTF8Encoding(true))){sw.WriteLine("pacient,data,inaltime_cm,varsta,sex,greutate_kg,imc,grasime_corporala,apa_corporala,puls");foreach(var r in rows)sw.WriteLine(string.Join(",",new[]{"\""+(r.patient_name??"").Replace("\"","\"\"")+"\"",r.measured_at,r.height_cm.ToString(CultureInfo.InvariantCulture),r.age.ToString(),r.sex,r.weight_kg.ToString(CultureInfo.InvariantCulture),r.bmi.ToString(CultureInfo.InvariantCulture),r.body_fat_percent.ToString(CultureInfo.InvariantCulture),r.body_water_percent.ToString(CultureInfo.InvariantCulture),r.heart_rate_bpm.ToString(CultureInfo.InvariantCulture)}));}}
        string RomanianStatus(string s){return (s??"").Replace("Looking for an FFB0-compatible scale","Se caută un cântar compatibil cu protocolul FFB0").Replace("briefly step on it to wake it","țineți un picior pe cântar pentru a-l menține activ").Replace("Scanning","Se caută cântarul").Replace("Connecting to FFB0 scale","Se conectează la cântarul FFB0").Replace("Opening scale service FFB0","Se deschide serviciul FFB0").Replace("attempt","încercarea").Replace("of 8","din 8").Replace("Keep the scale awake","Mențineți cântarul activ").Replace("Could not connect:","Conectarea nu a reușit:").Replace("Scale service FFB0 was unavailable after several attempts","Serviciul FFB0 nu a fost disponibil după mai multe încercări").Replace("Close Fitdays+ on nearby phones and keep one foot on the scale while connecting","Închideți Fitdays+ pe telefoanele din apropiere și țineți un picior pe cântar în timpul conectării").Replace("Not connected","Neconectat").Replace("Measurement","Măsurare").Replace("Scale found","Cântar găsit");}
        public void Dispose(){if(printManager!=null)printManager.PrintTaskRequested-=PrintTaskRequested;ble.Dispose();}
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if(args.Any(x=>x=="--report-self-test")||args.Any(x=>x=="--render-report-preview"))
            {
                try{var app=new Application();var clinic=new ClinicApplication();clinic.CreateWindow();if(args.Any(x=>x=="--render-report-preview")){int i=Array.IndexOf(args,"--render-report-preview");if(i<0||i+1>=args.Length)return 4;clinic.RenderReportPreview(args[i+1]);return 0;}return clinic.ReportSelfTest()?0:3;}catch{return 1;}
            }
            if(args.Any(x=>x=="--self-test"))
            {
                try{var p=new UserProfile{HeightCm=178,Age=18,Male=true};var r=BodyMath.Compute(63.85,500,93,p);if(Math.Abs(r.bmi-20.2)>.01)return 2;return 0;}catch{return 1;}
            }
            try{var app=new Application();var scale=new ClinicApplication();var window=scale.CreateWindow();if(args.Any(x=>x=="--native-print-probe")){int i=Array.IndexOf(args,"--native-print-probe");if(i<0||i+1>=args.Length)return 4;scale.StartNativePrintProbe(args[i+1]);}app.Run(window);return 0;}
            catch(Exception ex){MessageBox.Show(ex.ToString(),"Eroare la pornirea Cântar FFB0",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
        }
    }
}
