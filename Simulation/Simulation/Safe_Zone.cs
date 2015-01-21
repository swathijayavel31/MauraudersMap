using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class Safe_Zone
    {
        // PUBLIC CONSTANTS
        public enum states { ACTIVE = 0, REMOVE }


        // PRIVATE FIELDS
        private int id;
        private string name;
        private states state;
        private List<Tuple<double, double>> points;
        
        // PUBLIC PROPERTIES
        public int Id { get { return id; } }
        public string Name { get { return String.Copy(name); } set { name = String.Copy(value); } }
        public states State { get { return state; } set { state = value; } }
        public List<Tuple<double,double>> Points { get { return points; } }  // TODO:  Remove after test

        public Safe_Zone() {}

        public Safe_Zone(int id, string name, List<Tuple<double, double>> points)
        {
            this.id = id;
            this.name = String.Copy(name);
            state = states.ACTIVE;
            this.points = points;
        }

        public void append_point(double lat, double lng)
        {
            points.Add(new Tuple<double, double>(lat, lng));
        }

        public void append_points(List<Tuple<double, double>> points)
        {
            points.AddRange(points);
        }

        public Tuple<double, double> remove_last_point()
        {
            if (points.Count > 0) {
                Tuple<double, double> last_point = points[points.Count - 1];
                points.RemoveAt(points.Count - 1);
                return last_point;
            }
            else return null;
        }

        public void remove_all_points()
        {
            points.Clear();
        }

        public bool is_inside(double lat, double lng)
        {
            if (points.Count < 2) return false;
            else
            {
                int left_border_crosses = 0;
                int right_border_crosses = 0;

                double lat0 = points[0].Item1;
                double lng0 = points[0].Item2;

                bool on_border = false;
                int i = 1;

                while (!on_border && i<=points.Count) {

                    double lat1;
                    double lng1;

                    if (i < points.Count)
                    {
                        lat1 = points[i].Item1;
                        lng1 = points[i].Item2;
                    }
                    else
                    {
                        lat1 = points[0].Item1;
                        lng1 = points[0].Item2;
                    }

                    // Determe if intersection occurs
                    if (!(lat0 >= lat && lat1 >= lat) && !(lat0 <= lat && lat1 <= lat))
                    {
                        if (lng0 == lng1)
                        {
                            // Vertical line
                            if (lng0 == lng) on_border = true;
                            else if (lng0 < lng) left_border_crosses++;
                            else right_border_crosses++;
                        }
                        else if (lat0==lat1)
                        {
                            // Horizontal Line
                            double lng_min = Math.Min(lng0,lng1);
                            double lng_max = Math.Max(lng0,lng1);
                            if (lng_min <= lng && lng_max >= lng) on_border = true;
                            else if (lng_min < lng) left_border_crosses++;
                            else right_border_crosses++;
                        }
                        else
                        {
                            // Not vertical line
                            // Determine x of intersection
                            double m = (lat1 - lat0) / (lng1 - lng0);
                            double b = lat0 - m * lng0;
                            double x = (lat - b) / m;
                            if (x == lng) on_border = true;
                            else if (x < lng) left_border_crosses++;
                            else right_border_crosses++;
                        }
                    }

                    // Update variables
                    lat0 = lat1;
                    lng0 = lng1;
                    i++;
                }

                if (on_border) return true;
                else if (left_border_crosses % 2 == 1 && right_border_crosses % 2 == 1) return true;
                else return false;
            }
        }
    }
}