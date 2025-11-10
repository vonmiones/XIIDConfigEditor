using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIIDConfigEditor
{
    public class IniItem
    {
        public string Key { get; set; }
        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnValueChanged;
    }

}
