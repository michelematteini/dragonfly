using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A container for a set of field for which changes should be monitored.
    /// </summary>
    public abstract class ObservableRecord
    {
        public class Field<T>
        {
            protected T InnerValue;
            private ObservableRecord parent;

            public Field(ObservableRecord parent, T initialValue)
            {
                InnerValue = initialValue;
                this.parent = parent;          
                parent.fields.Add(this);
            }

            protected void InvalidateParent()
            {
                parent.Changed = true;
            }

            public virtual T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return InnerValue;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    if (InnerValue == null)
                    {
                        if (value != null)
                            InvalidateParent();
                    }
                    else if (!InnerValue.Equals(value))
                    {
                        InvalidateParent();
                    }

                    InnerValue = value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T(Field<T> field)
            {
                return field.InnerValue;
            }

            public override string ToString()
            {
                return InnerValue.ToString();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return InnerValue.GetHashCode();
            }
        }

        private List<object> fields;

        public ObservableRecord()
        {
            fields = new List<object>();
        }

        public Field<T> CreateField<T>(T initialValue)
        {
            Field<T> newField = new Field<T>(this, initialValue);
            fields.Add(newField);
            return newField;
        }

        public bool Changed { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(fields);    
        }

    }

    public class BoolField : ObservableRecord.Field<bool>
    {
        public BoolField(ObservableRecord parent, bool initialValue) : base(parent, initialValue)
        {
        }

        public override bool Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return InnerValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return InnerValue ? 1 : 0;
        }
    }

    public class IntField : ObservableRecord.Field<int>
    {
        public IntField(ObservableRecord parent, int initialValue) : base(parent, initialValue)
        {
        }

        public override int Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return InnerValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return InnerValue;
        }
    }

    public class StringField : ObservableRecord.Field<string>
    {
        public StringField(ObservableRecord parent, string initialValue) : base(parent, initialValue)
        {
        }

        public override string Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return InnerValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }
    }

}
