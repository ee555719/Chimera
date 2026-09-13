"""
Chimera Python Plugin Template
This is a sample Python plugin for Chimera.
"""

from chimera_sdk import ChimeraPlugin, ui, logger

# Create plugin instance
plugin = ChimeraPlugin()


@plugin.on_startup
def on_startup():
    """Called when the plugin is loaded"""
    logger.info("Python plugin loaded!")
    
    # Register a custom tab
    ui.register_tab(
        title="Python Plugin",
        elements=[
            {
                "type": "text",
                "content": "Hello from Python!"
            },
            {
                "type": "button",
                "label": "Click Me",
                "action": "python_plugin.button_click"
            }
        ]
    )


@plugin.on_shutdown
def on_shutdown():
    """Called when the plugin is unloaded"""
    logger.info("Python plugin unloaded!")


# Register custom handler
def handle_button_click(params):
    logger.info("Button was clicked!")
    return {"success": True}


plugin.register_handler("python_plugin.button_click", handle_button_click)


if __name__ == "__main__":
    plugin.run()
