﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a hollow sphere,
    /// or a sphere filled differently on inside and outside.
    /// The "shell" of the sphere is always 1 block wide. </summary>
    public sealed class SphereHollowDrawOperation : EllipsoidHollowDrawOperation {
        public override string Name {
            get { return "SphereH"; }
        }

        public SphereHollowDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].Z - marks[1].Z) * (marks[0].Z - marks[1].Z) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].Z = (short)Math.Round( marks[0].Z - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].Z = (short)Math.Round( marks[0].Z + radius );

            return base.Prepare( marks );
        }
    }
}