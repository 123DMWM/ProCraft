﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.AutoRank {
    sealed class Criterion : ICloneable {
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public ConditionSet Condition { get; set; }

        public Criterion() { }

        public Criterion( [NotNull] Criterion other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            FromRank = other.FromRank;
            ToRank = other.ToRank;
            Condition = other.Condition;
        }

        public Criterion( [NotNull] Rank fromRank, [NotNull] Rank toRank, [NotNull] ConditionSet condition ) {
            if( fromRank == null ) throw new ArgumentNullException( "fromRank" );
            if( toRank == null ) throw new ArgumentNullException( "toRank" );
            if( condition == null ) throw new ArgumentNullException( "condition" );
            FromRank = fromRank;
            ToRank = toRank;
            Condition = condition;
        }

        public Criterion( [NotNull] XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            FromRank = Rank.Parse( el.Attribute( "fromRank" ).Value );
            if( FromRank == null ) throw new FormatException( "Could not parse \"fromRank\"" );

            ToRank = Rank.Parse( el.Attribute( "toRank" ).Value );
            if( ToRank == null ) throw new FormatException( "Could not parse \"toRank\"" );

            Condition = (ConditionSet)AutoRank.Condition.Parse( el.Elements().First() );
        }

        public object Clone() {
            return new Criterion( this );
        }

        public override string ToString() {
            return String.Format( "Criteria( {0} from {1} to {2} )",
                                  (FromRank < ToRank ? "promote" : "demote"),
                                  FromRank.Name,
                                  ToRank.Name );
        }

        public XElement Serialize() {
            XElement el = new XElement( "Criterion" );
            el.Add( new XAttribute( "fromRank", FromRank.FullName ) );
            el.Add( new XAttribute( "toRank", ToRank.FullName ) );
            if( Condition != null ) {
                el.Add( Condition.Serialize() );
            }
            return el;
        }
    }
}
