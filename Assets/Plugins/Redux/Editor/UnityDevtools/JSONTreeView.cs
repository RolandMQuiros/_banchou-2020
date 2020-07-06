using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.IMGUI.Controls;

using Newtonsoft.Json.Linq;

namespace Redux.UnityEditor {
    public class JSONTreeView : TreeView {
        public JToken Source { get; set; } = null;

        public JSONTreeView(TreeViewState treeViewState) : base(treeViewState) { }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();

            if (Source != null) {
                var stack = new Stack<(JToken Token, int Depth)>();
                var idCounter = 1;
                stack.Push((Token: Source, Depth: 0));

                while (stack.Count > 0) {
                    var (token, depth) = stack.Pop();

                    var skip = token.Parent?.Type == JTokenType.Property || // Skip if parent is a property, since we draw the value in the same row
                        (token.Type == JTokenType.Property && ((JProperty)token).Name == "$type"); // Skip type property

                    if (!skip) {
                        allItems.Add(
                            new JSONTreeItem(
                                id: idCounter++,
                                depth: depth++,
                                token: token
                            )
                        );
                    }

                    foreach (var child in token.Children()) {
                        stack.Push((Token: child, Depth: depth));
                    }
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args) {
            var item = (JSONTreeItem)args.item;
            var indent = GetContentIndent(item);

            var rect = new Rect(args.rowRect) {
                x = args.rowRect.x + indent
            };

            var _rowStyle = new GUIStyle {
                richText = true,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };

            var value = TokenValueString(item.Token);
            if (!string.IsNullOrWhiteSpace(value)) {
                GUI.Label(rect, args.selected ? $"<b>{value}</b>" : value, _rowStyle);
            }

            string TokenValueString(JToken token) {
                switch (token.Type) {
                    case JTokenType.Property:
                        var property = (JProperty)token;
                        if (property.Value.Type != JTokenType.Property) {
                            var propValue = TokenValueString(property.Value);
                            if (!string.IsNullOrWhiteSpace(propValue)) {
                                return $"\"{property.Name}\": {propValue}";
                            }
                        }
                        break;
                    case JTokenType.Array:
                        return $"<color=#03f4fc>[</color>{(((JArray)token).Count > 0 ? " ... " : " ")}<color=#03f4fc>]</color>";
                    case JTokenType.Object: {
                        var obj = (JObject)token;
                        var typeProperty = obj.Property("$type");

                        string typeName = " ";
                        if (args.selected && typeProperty != null && typeProperty.Value.Type == JTokenType.String) {
                            typeName = $" {typeProperty.Value} ";
                        } else if (((JObject)token).Properties().Any()) {
                            typeName = " ... ";
                        }

                        return $"<color=#00ffc8>{{</color>{typeName}<color=#00ffc8>}}</color>";
                    }
                    case JTokenType.Float:
                    case JTokenType.Integer:
                        return $"<color=#00ff6e>{token}</color>";
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                    case JTokenType.String:
                        return $"<color=#ff006f>\"{token.ToString()}\"</color>";
                    case JTokenType.Boolean:
                        return $"<color=#e1ff00>{token}</color>";
                    case JTokenType.Null:
                    case JTokenType.None:
                        return $"<color=#0076de>null</color>";
                }
                return null;
            }
        }
    }

    internal class JSONTreeItem : TreeViewItem {
        public JToken Token { get; private set; }
        public JSONTreeItem(int id, int depth, JToken token) : base(id, depth) {
            Token = token;
        }
    }
}