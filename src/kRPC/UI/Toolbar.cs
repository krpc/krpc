/*
Copyright (c) 2013-2014, Maik Schreiber
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KRPC.UI {



    /**********************************************************\
    *          --- DO NOT EDIT BELOW THIS COMMENT ---          *
    *                                                          *
    * This file contains classes and interfaces to use the     *
    * Toolbar Plugin without creating a hard dependency on it. *
    *                                                          *
    * There is nothing in this file that needs to be edited    *
    * by hand.                                                 *
    *                                                          *
    *          --- DO NOT EDIT BELOW THIS COMMENT ---          *
    \**********************************************************/



    /// <summary>
    /// The global tool bar manager.
    /// </summary>
    public partial class ToolbarManager : IToolbarManager {
        /// <summary>
        /// Whether the Toolbar Plugin is available.
        /// </summary>
        public static bool ToolbarAvailable {
            get {
                if (toolbarAvailable == null) {
                    toolbarAvailable = Instance != null;
                }
                return (bool) toolbarAvailable;
            }
        }

        /// <summary>
        /// The global tool bar manager instance.
        /// </summary>
        public static IToolbarManager Instance {
            get {
                if ((toolbarAvailable != false) && (instance_ == null)) {
                    Type type = AssemblyLoader.loadedAssemblies
                        .SelectMany(a => a.assembly.GetExportedTypes())
                        .SingleOrDefault(t => t.FullName == "Toolbar.ToolbarManager");
                    if (type != null) {
                        object realToolbarManager = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                        instance_ = new ToolbarManager(realToolbarManager);
                    }
                }
                return instance_;
            }
        }
    }

    #region interfaces

    /// <summary>
    /// A toolbar manager.
    /// </summary>
    public interface IToolbarManager {
        /// <summary>
        /// Adds a new button.
        /// </summary>
        /// <remarks>
        /// To replace an existing button, just add a new button using the old button's namespace and ID.
        /// Note that the new button will inherit the screen position of the old button.
        /// </remarks>
        /// <param name="ns">The new button's namespace. This is usually the plugin's name. Must not include special characters like '.'</param>
        /// <param name="id">The new button's ID. This ID must be unique across all buttons in the namespace. Must not include special characters like '.'</param>
        /// <returns>The button created.</returns>
        IButton add(string ns, string id);
    }

    /// <summary>
    /// Represents a clickable button.
    /// </summary>
    public interface IButton {
        /// <summary>
        /// The text displayed on the button. Set to null to hide text.
        /// </summary>
        /// <remarks>
        /// The text can be changed at any time to modify the button's appearance. Note that since this will also
        /// modify the button's size, this feature should be used sparingly, if at all.
        /// </remarks>
        /// <seealso cref="TexturePath"/>
        string Text {
            set;
            get;
        }

        /// <summary>
        /// The color the button text is displayed with. Defaults to Color.white.
        /// </summary>
        /// <remarks>
        /// The text color can be changed at any time to modify the button's appearance.
        /// </remarks>
        Color TextColor {
            set;
            get;
        }

        /// <summary>
        /// The path of a texture file to display an icon on the button. Set to null to hide icon.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A texture path on a button will have precedence over text. That is, if both text and texture path
        /// have been set on a button, the button will show the texture, not the text.
        /// </para>
        /// <para>
        /// The texture size must not exceed 24x24 pixels.
        /// </para>
        /// <para>
        /// The texture path must be relative to the "GameData" directory, and must not specify a file name suffix.
        /// Valid example: MyAddon/Textures/icon_mybutton
        /// </para>
        /// <para>
        /// The texture path can be changed at any time to modify the button's appearance.
        /// </para>
        /// </remarks>
        /// <seealso cref="Text"/>
        string TexturePath {
            set;
            get;
        }

        /// <summary>
        /// The button's tool tip text. Set to null if no tool tip is desired.
        /// </summary>
        /// <remarks>
        /// Tool Tip Text Should Always Use Headline Style Like This.
        /// </remarks>
        string ToolTip {
            set;
            get;
        }

        /// <summary>
        /// Whether this button is currently visible or not. Can be used in addition to or as a replacement for <see cref="Visibility"/>.
        /// </summary>
        /// <remarks>
        /// Setting this property to true does not affect the player's ability to hide the button using the configuration.
        /// Conversely, setting this property to false does not enable the player to show the button using the configuration.
        /// </remarks>
        bool Visible {
            set;
            get;
        }

        /// <summary>
        /// Determines this button's visibility. Can be used in addition to or as a replacement for <see cref="Visible"/>.
        /// </summary>
        /// <remarks>
        /// The return value from IVisibility.Visible is subject to the same rules as outlined for
        /// <see cref="Visible"/>.
        /// </remarks>
        IVisibility Visibility {
            set;
            get;
        }

        /// <summary>
        /// Whether this button is currently effectively visible or not. This is a combination of
        /// <see cref="Visible"/> and <see cref="Visibility"/>.
        /// </summary>
        /// <remarks>
        /// Note that the toolbar is not visible in certain game scenes, for example the loading screens. This property
        /// does not reflect button invisibility in those scenes. In addition, this property does not reflect the
        /// player's configuration of the button's visibility.
        /// </remarks>
        bool EffectivelyVisible {
            get;
        }

        /// <summary>
        /// Whether this button is currently enabled (clickable) or not. This does not affect the player's ability to
        /// position the button on their toolbar.
        /// </summary>
        bool Enabled {
            set;
            get;
        }

        /// <summary>
        /// Whether this button is currently "important." Set to false to return to normal button behaviour.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be used to temporarily force the button to be shown on screen regardless of the toolbar being
        /// currently in auto-hidden mode. For example, a button that signals the arrival of a private message in
        /// a chat room could mark itself as "important" as long as the message has not been read.
        /// </para>
        /// <para>
        /// Setting this property does not change the appearance of the button. Use <see cref="TexturePath"/> to
        /// change the button's icon.
        /// </para>
        /// <para>
        /// Setting this property to true does not affect the player's ability to hide the button using the
        /// configuration.
        /// </para>
        /// <para>
        /// This feature should be used only sparingly, if at all, since it forces the button to be displayed on
        /// screen even when it normally wouldn't.
        /// </para>
        /// </remarks>
        bool Important {
            set;
            get;
        }

        /// <summary>
        /// Event handler that can be registered with to receive "on click" events.
        /// </summary>
        /// <example>
        /// <code>
        /// IButton button = ...
        /// button.OnClick += (e) => {
        ///     Debug.Log("button clicked, mouseButton: " + e.MouseButton);
        /// };
        /// </code>
        /// </example>
        event ClickHandler OnClick;

        /// <summary>
        /// Event handler that can be registered with to receive "on mouse enter" events.
        /// </summary>
        /// <example>
        /// <code>
        /// IButton button = ...
        /// button.OnMouseEnter += (e) => {
        ///     Debug.Log("mouse entered button");
        /// };
        /// </code>
        /// </example>
        event MouseEnterHandler OnMouseEnter;

        /// <summary>
        /// Event handler that can be registered with to receive "on mouse leave" events.
        /// </summary>
        /// <example>
        /// <code>
        /// IButton button = ...
        /// button.OnMouseLeave += (e) => {
        ///     Debug.Log("mouse left button");
        /// };
        /// </code>
        /// </example>
        event MouseLeaveHandler OnMouseLeave;

        /// <summary>
        /// Permanently destroys this button so that it is no longer displayed.
        /// Should be used when a plugin is stopped to remove leftover buttons.
        /// </summary>
        void Destroy();
    }

    #endregion

    #region events

    /// <summary>
    /// Event describing a click on a button.
    /// </summary>
    public partial class ClickEvent : EventArgs {
        /// <summary>
        /// The button that has been clicked.
        /// </summary>
        public readonly IButton Button;

        /// <summary>
        /// The mouse button which the button was clicked with.
        /// </summary>
        /// <remarks>
        /// Is 0 for left mouse button, 1 for right mouse button, and 2 for middle mouse button.
        /// </remarks>
        public readonly int MouseButton;
    }

    /// <summary>
    /// An event handler that is invoked whenever a button has been clicked.
    /// </summary>
    /// <param name="e">An event describing the button click.</param>
    public delegate void ClickHandler(ClickEvent e);

    /// <summary>
    /// Event describing the mouse pointer moving about a button.
    /// </summary>
    public abstract partial class MouseMoveEvent {
        /// <summary>
        /// The button in question.
        /// </summary>
        public readonly IButton button;
    }

    /// <summary>
    /// Event describing the mouse pointer entering a button's area.
    /// </summary>
    public partial class MouseEnterEvent : MouseMoveEvent {
    }

    /// <summary>
    /// Event describing the mouse pointer leaving a button's area.
    /// </summary>
    public partial class MouseLeaveEvent : MouseMoveEvent {
    }

    /// <summary>
    /// An event handler that is invoked whenever the mouse pointer enters a button's area.
    /// </summary>
    /// <param name="e">An event describing the mouse pointer entering.</param>
    public delegate void MouseEnterHandler(MouseEnterEvent e);

    /// <summary>
    /// An event handler that is invoked whenever the mouse pointer leaves a button's area.
    /// </summary>
    /// <param name="e">An event describing the mouse pointer leaving.</param>
    public delegate void MouseLeaveHandler(MouseLeaveEvent e);

    #endregion

    #region visibility

    /// <summary>
    /// Determines visibility of a button.
    /// </summary>
    /// <seealso cref="IButton.Visibility"/>
    public interface IVisibility {
        /// <summary>
        /// Whether a button is currently visible or not.
        /// </summary>
        /// <seealso cref="IButton.Visible"/>
        bool Visible {
            get;
        }
    }

    /// <summary>
    /// Determines visibility of a button in relation to the currently running game scene.
    /// </summary>
    /// <example>
    /// <code>
    /// IButton button = ...
    /// button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);
    /// </code>
    /// </example>
    /// <seealso cref="IButton.Visibility"/>
    public class GameScenesVisibility : IVisibility {
        private GameScenes[] gameScenes;

        public bool Visible {
            get {
                return (bool) visibleProperty.GetValue(realGameScenesVisibility, null);
            }
        }

        private object realGameScenesVisibility;
        private PropertyInfo visibleProperty;

        public GameScenesVisibility(params GameScenes[] gameScenes) {
            Type gameScenesVisibilityType = AssemblyLoader.loadedAssemblies
                .SelectMany(a => a.assembly.GetExportedTypes())
                .SingleOrDefault(t => t.FullName == "Toolbar.GameScenesVisibility");
            realGameScenesVisibility = Activator.CreateInstance(gameScenesVisibilityType, new object[] { gameScenes });
            visibleProperty = gameScenesVisibilityType.GetProperty("Visible", BindingFlags.Public | BindingFlags.Instance);
            this.gameScenes = gameScenes;
        }
    }

    #endregion

    #region private implementations

    public partial class ToolbarManager : IToolbarManager {
        private static bool? toolbarAvailable = null;
        private static IToolbarManager instance_;

        private object realToolbarManager;
        private MethodInfo addMethod;
        private Dictionary<object, IButton> buttons = new Dictionary<object, IButton>();
        private Type iButtonType;
        private Type functionVisibilityType;

        private ToolbarManager(object realToolbarManager) {
            this.realToolbarManager = realToolbarManager;

            Type iToolbarManagerType = AssemblyLoader.loadedAssemblies
                .SelectMany(a => a.assembly.GetExportedTypes())
                .SingleOrDefault(t => t.FullName == "Toolbar.IToolbarManager");
            addMethod = iToolbarManagerType.GetMethod("add", BindingFlags.Public | BindingFlags.Instance);

            iButtonType = AssemblyLoader.loadedAssemblies
                .SelectMany(a => a.assembly.GetExportedTypes())
                .SingleOrDefault(t => t.FullName == "Toolbar.IButton");
            functionVisibilityType = AssemblyLoader.loadedAssemblies
                .SelectMany(a => a.assembly.GetExportedTypes())
                .SingleOrDefault(t => t.FullName == "Toolbar.FunctionVisibility");
        }

        public IButton add(string ns, string id) {
            object realButton = addMethod.Invoke(realToolbarManager, new object[] { ns, id });
            IButton button = new Button(realButton, iButtonType, functionVisibilityType);
            buttons.Add(realButton, button);
            return button;
        }
    }

    internal class Button : IButton {
        private object realButton;
        private PropertyInfo textProperty;
        private PropertyInfo textColorProperty;
        private PropertyInfo texturePathProperty;
        private PropertyInfo toolTipProperty;
        private PropertyInfo visibleProperty;
        private PropertyInfo visibilityProperty;
        private Type functionVisibilityType;
        private PropertyInfo effectivelyVisibleProperty;
        private PropertyInfo enabledProperty;
        private PropertyInfo importantProperty;
        private EventInfo onClickEvent;
        private Delegate realClickHandler;
        private EventInfo onMouseEnterEvent;
        private Delegate realMouseEnterHandler;
        private EventInfo onMouseLeaveEvent;
        private Delegate realMouseLeaveHandler;
        private MethodInfo destroyMethod;

        internal Button(object realButton, Type iButtonType, Type functionVisibilityType) {
            this.realButton = realButton;
            this.functionVisibilityType = functionVisibilityType;

            textProperty = iButtonType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
            textColorProperty = iButtonType.GetProperty("TextColor", BindingFlags.Public | BindingFlags.Instance);
            texturePathProperty = iButtonType.GetProperty("TexturePath", BindingFlags.Public | BindingFlags.Instance);
            toolTipProperty = iButtonType.GetProperty("ToolTip", BindingFlags.Public | BindingFlags.Instance);
            visibleProperty = iButtonType.GetProperty("Visible", BindingFlags.Public | BindingFlags.Instance);
            visibilityProperty = iButtonType.GetProperty("Visibility", BindingFlags.Public | BindingFlags.Instance);
            effectivelyVisibleProperty = iButtonType.GetProperty("EffectivelyVisible", BindingFlags.Public | BindingFlags.Instance);
            enabledProperty = iButtonType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
            importantProperty = iButtonType.GetProperty("Important", BindingFlags.Public | BindingFlags.Instance);
            onClickEvent = iButtonType.GetEvent("OnClick", BindingFlags.Public | BindingFlags.Instance);
            onMouseEnterEvent = iButtonType.GetEvent("OnMouseEnter", BindingFlags.Public | BindingFlags.Instance);
            onMouseLeaveEvent = iButtonType.GetEvent("OnMouseLeave", BindingFlags.Public | BindingFlags.Instance);
            destroyMethod = iButtonType.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Instance);

            realClickHandler = attachEventHandler(onClickEvent, "clicked", realButton);
            realMouseEnterHandler = attachEventHandler(onMouseEnterEvent, "mouseEntered", realButton);
            realMouseLeaveHandler = attachEventHandler(onMouseLeaveEvent, "mouseLeft", realButton);
        }

        private Delegate attachEventHandler(EventInfo @event, string methodName, object realButton) {
            MethodInfo method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate d = Delegate.CreateDelegate(@event.EventHandlerType, this, method);
            @event.GetAddMethod().Invoke(realButton, new object[] { d });
            return d;
        }

        public string Text {
            set {
                textProperty.SetValue(realButton, value, null);
            }
            get {
                return (string) textProperty.GetValue(realButton, null);
            }
        }

        public Color TextColor {
            set {
                textColorProperty.SetValue(realButton, value, null);
            }
            get {
                return (Color) textColorProperty.GetValue(realButton, null);
            }
        }

        public string TexturePath {
            set {
                texturePathProperty.SetValue(realButton, value, null);
            }
            get {
                return (string) texturePathProperty.GetValue(realButton, null);
            }
        }

        public string ToolTip {
            set {
                toolTipProperty.SetValue(realButton, value, null);
            }
            get {
                return (string) toolTipProperty.GetValue(realButton, null);
            }
        }

        public bool Visible {
            set {
                visibleProperty.SetValue(realButton, value, null);
            }
            get {
                return (bool) visibleProperty.GetValue(realButton, null);
            }
        }

        public IVisibility Visibility {
            set {
                object functionVisibility = Activator.CreateInstance(functionVisibilityType, new object[] { new Func<bool>(() => value.Visible) });
                visibilityProperty.SetValue(realButton, functionVisibility, null);
                visibility_ = value;
            }
            get {
                return visibility_;
            }
        }
        private IVisibility visibility_;

        public bool EffectivelyVisible {
            get {
                return (bool) effectivelyVisibleProperty.GetValue(realButton, null);
            }
        }

        public bool Enabled {
            set {
                enabledProperty.SetValue(realButton, value, null);
            }
            get {
                return (bool) enabledProperty.GetValue(realButton, null);
            }
        }

        public bool Important {
            set {
                importantProperty.SetValue(realButton, value, null);
            }
            get {
                return (bool) importantProperty.GetValue(realButton, null);
            }
        }

        public event ClickHandler OnClick;

        private void clicked(object realEvent) {
            if (OnClick != null) {
                OnClick(new ClickEvent(realEvent, this));
            }
        }

        public event MouseEnterHandler OnMouseEnter;

        private void mouseEntered(object realEvent) {
            if (OnMouseEnter != null) {
                OnMouseEnter(new MouseEnterEvent(this));
            }
        }

        public event MouseLeaveHandler OnMouseLeave;

        private void mouseLeft(object realEvent) {
            if (OnMouseLeave != null) {
                OnMouseLeave(new MouseLeaveEvent(this));
            }
        }

        public void Destroy() {
            detachEventHandler(onClickEvent, realClickHandler, realButton);
            detachEventHandler(onMouseEnterEvent, realMouseEnterHandler, realButton);
            detachEventHandler(onMouseLeaveEvent, realMouseLeaveHandler, realButton);

            destroyMethod.Invoke(realButton, null);
        }

        private void detachEventHandler(EventInfo @event, Delegate d, object realButton) {
            @event.GetRemoveMethod().Invoke(realButton, new object[] { d });
        }

        private Delegate createDelegate(Type eventHandlerType, string methodName) {
            return Delegate.CreateDelegate(GetType(), GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance));
        }
    }

    public partial class ClickEvent : EventArgs {
        internal ClickEvent(object realEvent, IButton button) {
            Type type = realEvent.GetType();
            Button = button;
            MouseButton = (int) type.GetField("MouseButton", BindingFlags.Public | BindingFlags.Instance).GetValue(realEvent);
        }
    }

    public abstract partial class MouseMoveEvent : EventArgs {
        internal MouseMoveEvent(IButton button) {
            this.button = button;
        }
    }

    public partial class MouseEnterEvent : MouseMoveEvent {
        internal MouseEnterEvent(IButton button)
            : base(button) {
        }
    }

    public partial class MouseLeaveEvent : MouseMoveEvent {
        internal MouseLeaveEvent(IButton button)
            : base(button) {
        }
    }

    #endregion
}
