﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Indexes available brushes.
    /// Provides /Brush command, a way to register new brushes,
    /// a way to look up existing brushes by name. </summary>
    public static class BrushManager {
        static readonly Dictionary<string, IBrushFactory> BrushFactories = new Dictionary<string, IBrushFactory>();
        static readonly Dictionary<string, IBrushFactory> BrushAliases = new Dictionary<string, IBrushFactory>();

        public static readonly CommandDescriptor CdBrush = new CommandDescriptor {
            Name = "Brush",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Gets or sets the current brush. Available brushes are: ",
            HelpSections = new Dictionary<string, string>(), // filled by RegisterBrush
            Handler = BrushHandler
        };


        static void BrushHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            string brushName = cmd.Next();
            if (brushName == null) {
                player.Message("Brush: {0}", player.BrushDescription);
            } else {
                IBrushFactory brushFactory = GetBrushFactory(brushName);
                if (brushFactory == null) {
                    player.Message("Unrecognized brush \"{0}\"", brushName);
                } else {
                    player.BrushSet(brushFactory);
                    if (cmd.HasNext) {
                        player.ConfigureBrush(cmd);
                    }
                    player.Message("Brush set to {0}", player.BrushDescription);
                }
            }
        }


        internal static void Init() {
            CommandManager.RegisterCommand(CdBrush);
            RegisterBrush(NormalBrushFactory.Instance);
            RegisterBrush(CheckeredBrushFactory.Instance);
            RegisterBrush(RandomBrushFactory.Instance);
            RegisterBrush(BWRainbowBrush.Instance);
            RegisterBrush(StripedBrushFactory.Instance);
            RegisterBrush(RainbowBrush.Instance);
            RegisterBrush(CloudyBrushFactory.Instance);
            RegisterBrush(ReplaceBrushFactory.Instance);
            RegisterBrush(ReplaceNotBrushFactory.Instance);
            RegisterBrush(ReplaceBrushBrushFactory.Instance);
            RegisterBrush(ReplaceNotBrushBrushFactory.Instance);
            RegisterBrush(PasteBrushFactory.PasteInstance);
            RegisterBrush(PasteBrushFactory.PasteNotInstance);
        }


        /// <summary> Registers a new brush. </summary>
        /// <param name="factory"> IBrushFactory that will be used to create new instances of the brush. </param>
        /// <exception cref="ArgumentNullException"> factory is null. </exception>
        /// <exception cref="ArgumentException"> brush with the same name or alias already exists. </exception>
        public static void RegisterBrush([NotNull] IBrushFactory factory) {
            if (factory == null)
                throw new ArgumentNullException("factory");
            string helpString = String.Format("&H{0} brush:&N&S{1}",
                                              factory.Name,
                                              factory.Help);
            string lowerName = factory.Name.ToLower();
            BrushFactories.Add(lowerName, factory);
            if (factory.Aliases != null) {
                helpString += "&N&HAliases: " + factory.Aliases.JoinToString();
                foreach (string alias in factory.Aliases) {
                    BrushAliases.Add(alias.ToLower(), factory);
                }
            }
            CdBrush.HelpSections.Add(lowerName, helpString);
            CdBrush.Help += factory.Name + " ";
        }


        /// <summary> Finds IBrushFactory for given brush name or alias.
        /// Case-insensitive. Does not autocomplete names. </summary>
        /// <param name="brushName"> Brush name. </param>
        /// <returns> IBrushFactory if brush was found; otherwise null. </returns>
        /// <exception cref="ArgumentNullException"> brushName is null. </exception>
        [CanBeNull]
        public static IBrushFactory GetBrushFactory([NotNull] string brushName) {
            if (brushName == null)
                throw new ArgumentNullException("brushName");
            IBrushFactory factory;
            string lowerName = brushName.ToLower();
            if (BrushFactories.TryGetValue(lowerName, out factory) ||
                BrushAliases.TryGetValue(lowerName, out factory)) {
                return factory;
            } else {
                return null;
            }
        }


        /// <summary> Provides a list of all registered IBrushFactories. </summary>
        [NotNull]
        public static IBrushFactory[] RegisteredFactories {
            get { return BrushFactories.Values.ToArray(); }
        }
    }
}