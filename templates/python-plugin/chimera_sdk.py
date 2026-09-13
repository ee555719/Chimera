"""
Chimera Python SDK
Provides JSON-RPC 2.0 communication with the Chimera host process.
"""

import json
import sys
import threading
from typing import Any, Callable, Dict, List, Optional


class ChimeraPlugin:
    """
    Main plugin class for Chimera Python plugins.
    """
    
    def __init__(self):
        self._plugin_id = sys.argv[sys.argv.index("--plugin-id") + 1] if "--plugin-id" in sys.argv else "unknown"
        self._handlers: Dict[str, Callable] = {}
        self._startup_callback: Optional[Callable] = None
        self._shutdown_callback: Optional[Callable] = None
        
        # Register built-in handlers
        self._handlers["plugin.startup"] = self._handle_startup
        self._handlers["plugin.shutdown"] = self._handle_shutdown
    
    def on_startup(self, func: Callable):
        """Decorator for startup handler"""
        self._startup_callback = func
        return func
    
    def on_shutdown(self, func: Callable):
        """Decorator for shutdown handler"""
        self._shutdown_callback = func
        return func
    
    def register_handler(self, method: str, handler: Callable):
        """Register a custom RPC handler"""
        self._handlers[method] = handler
    
    def run(self):
        """Main event loop - reads JSON-RPC from stdin and writes responses to stdout"""
        for line in sys.stdin:
            line = line.strip()
            if not line:
                continue
            
            try:
                request = json.loads(line)
                response = self._process_request(request)
                if response is not None:
                    sys.stdout.write(json.dumps(response) + "\n")
                    sys.stdout.flush()
            except Exception as e:
                error_response = {
                    "jsonrpc": "2.0",
                    "error": {
                        "code": -32603,
                        "message": str(e)
                    },
                    "id": request.get("id") if isinstance(request, dict) else None
                }
                sys.stdout.write(json.dumps(error_response) + "\n")
                sys.stdout.flush()
    
    def _process_request(self, request: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Process a JSON-RPC request"""
        method = request.get("method", "")
        params = request.get("params", {})
        request_id = request.get("id")
        
        if method in self._handlers:
            try:
                result = self._handlers[method](params)
                if request_id is not None:
                    return {
                        "jsonrpc": "2.0",
                        "result": result,
                        "id": request_id
                    }
            except Exception as e:
                if request_id is not None:
                    return {
                        "jsonrpc": "2.0",
                        "error": {
                            "code": -32000,
                            "message": str(e)
                        },
                        "id": request_id
                    }
        else:
            if request_id is not None:
                return {
                    "jsonrpc": "2.0",
                    "error": {
                        "code": -32601,
                        "message": f"Method not found: {method}"
                    },
                    "id": request_id
                }
        
        return None
    
    def _handle_startup(self, params: Dict[str, Any]) -> Any:
        """Handle plugin startup"""
        if self._startup_callback:
            return self._startup_callback()
        return None
    
    def _handle_shutdown(self, params: Dict[str, Any]) -> Any:
        """Handle plugin shutdown"""
        if self._shutdown_callback:
            return self._shutdown_callback()
        return None


class UI:
    """UI operations for Python plugins"""
    
    @staticmethod
    def register_tab(title: str, elements: List[Dict[str, Any]]) -> Any:
        """Register a custom tab in the UI"""
        return _send_request("host.ui.register_tab", {
            "title": title,
            "elements": elements
        })
    
    @staticmethod
    def inject_content(target: str, content: str) -> Any:
        """Inject content into a UI area"""
        return _send_request("host.ui.inject_content", {
            "target": target,
            "content": content
        })
    
    @staticmethod
    def customize(styles: Dict[str, str]) -> Any:
        """Customize UI styles"""
        return _send_request("host.ui.customize", styles)


class FileSystem:
    """File system operations for Python plugins"""
    
    @staticmethod
    def request_write(path: str, content: str, timeout: int = 10) -> Any:
        """Request to write a file (with user confirmation)"""
        return _send_request("host.fs.request_write", {
            "path": path,
            "content": content,
            "timeout": timeout
        })
    
    @staticmethod
    def request_delete(path: str, timeout: int = 10) -> Any:
        """Request to delete a file (with user confirmation)"""
        return _send_request("host.fs.request_delete", {
            "path": path,
            "timeout": timeout
        })


class Process:
    """Process operations for Python plugins"""
    
    @staticmethod
    def request_shutdown() -> Any:
        """Request application shutdown"""
        return _send_request("host.process.request_shutdown", {})
    
    @staticmethod
    def request_launch(command: str, args: List[str] = None) -> Any:
        """Request to launch an external program"""
        return _send_request("host.process.request_launch", {
            "command": command,
            "args": args or []
        })


class Logger:
    """Logging operations for Python plugins"""
    
    @staticmethod
    def debug(message: str):
        """Log a debug message"""
        _send_request("host.log", {"level": "debug", "message": message})
    
    @staticmethod
    def info(message: str):
        """Log an info message"""
        _send_request("host.log", {"level": "info", "message": message})
    
    @staticmethod
    def warning(message: str):
        """Log a warning message"""
        _send_request("host.log", {"level": "warning", "message": message})
    
    @staticmethod
    def error(message: str, exception: Exception = None):
        """Log an error message"""
        _send_request("host.log", {
            "level": "error",
            "message": message,
            "exception": str(exception) if exception else None
        })


def _send_request(method: str, params: Dict[str, Any]) -> Any:
    """Send a JSON-RPC request to the host and wait for response"""
    import random
    
    request_id = random.randint(1, 1000000)
    request = {
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": request_id
    }
    
    sys.stdout.write(json.dumps(request) + "\n")
    sys.stdout.flush()
    
    # Note: In a real implementation, this would need to handle
    # asynchronous responses from stdin. For simplicity, this is
    # a synchronous implementation that assumes the host responds
    # immediately.
    # TODO: Implement proper async response handling
    return None


# Module-level instances for easy import
ui = UI()
fs = FileSystem()
process = Process()
logger = Logger()
