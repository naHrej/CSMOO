using System;

namespace CSMOO.Resources
{
    /// <summary>
    /// Help metadata for categories and topics
    /// This class defines descriptions and summaries for help categories and topics
    /// </summary>
    public class HelpMetadata
    {
        /// <summary>
        /// General help preamble - displayed when user types 'help' with no arguments
        /// </summary>
        /// <description>
        /// Welcome to the help system! 
        /// Use '<span class='command'>help</span> <span class='param'>&lt;category&gt;</span>' to see commands in a category,
        /// '<span class='command'>help</span> <span class='param'>&lt;topic&gt;</span>' to see related commands, 
        /// or '<span class='command'>help</span> <span class='param'>&lt;command&gt;</span>' for detailed information.
        /// </description>
        public static void _help_preamble() { }
        /// <summary>
        /// Communication category - Commands for interacting with other players
        /// </summary>
        /// <category>communication</category>
        /// <description>
        /// The communication category contains all commands related to talking to other players,
        /// sending messages, and social interactions in the game.
        /// Use '<span class='command'>help</span> <span class='param'>&lt;verb&gt;</span>' to get help on specific commands.
        /// </description>
        public static void _category_communication() { }
        
        /// <summary>
        /// Movement category - Commands for navigating the game world
        /// </summary>
        /// <category>movement</category>
        /// <description>
        /// Commands for moving between rooms and exploring the game world.
        /// </description>
        public static void _category_movement() { }
        
        /// <summary>
        /// Objects category - Commands for interacting with items and objects
        /// </summary>
        /// <category>objects</category>
        /// <description>
        /// Commands for picking up, dropping, examining, and manipulating objects in the game world.
        /// </description>
        public static void _category_objects() { }
        
        /// <summary>
        /// Basics category - Essential commands for new players
        /// </summary>
        /// <category>basics</category>
        /// <description>
        /// Essential commands that every player should know to get started in the game.
        /// </description>
        public static void _category_basics() { }
        
        /// <summary>
        /// Navigation topic - Finding your way around
        /// </summary>
        /// <topic>navigation</topic>
        /// <description>
        /// Commands and functions related to finding your way, viewing exits, and understanding your location.
        /// </description>
        public static void _topic_navigation() { }
        
        /// <summary>
        /// Social topic - Interacting with other players
        /// </summary>
        /// <topic>social</topic>
        /// <description>
        /// Commands for social interactions, talking, and communicating with other players.
        /// </description>
        public static void _topic_social() { }
        
        /// <summary>
        /// Examination topic - Looking at and inspecting objects
        /// </summary>
        /// <topic>examination</topic>
        /// <description>
        /// Commands for examining objects, rooms, and other players to get detailed information.
        /// </description>
        public static void _topic_examination() { }
        
        /// <summary>
        /// Inventory topic - Managing your possessions
        /// </summary>
        /// <topic>inventory</topic>
        /// <description>
        /// Commands for managing your inventory, picking up items, and dropping objects.
        /// </description>
        public static void _topic_inventory() { }
    }
}
