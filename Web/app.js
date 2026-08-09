(function() {
    console.log("[FastSTRM] Initialization started.");

    // Intercept Fetch
    const originalFetch = window.fetch;
    window.fetch = async function(resource, options) {
        let url = resource;
        if (typeof resource === 'object' && resource instanceof Request) {
            url = resource.url;
        }

        if (typeof url === 'string' && url.indexOf('/PlaybackInfo') !== -1 && url.indexOf('/FastSTRM/') === -1) {
            const match = url.match(/\/Items\/([a-zA-Z0-9]+)\/PlaybackInfo/i);
            if (match && match[1]) {
                const itemId = match[1];
                console.log(`[FastSTRM] Intercepted Fetch PlaybackInfo request for ItemId: ${itemId}`);
                
                const urlObj = new URL(url, window.location.origin);
                const queryParams = urlObj.search;
                const originalUrlEnc = encodeURIComponent(urlObj.pathname + urlObj.search);
                
                url = `/FastSTRM/GetMockedPlaybackInfo?itemId=${itemId}&originalUrl=${originalUrlEnc}&${queryParams.substring(1)}`;
                
                if (typeof resource === 'object' && resource instanceof Request) {
                    resource = new Request(url, resource);
                } else {
                    resource = url;
                }
            }
        }
        return originalFetch.call(this, resource, options);
    };

    // Intercept XHR
    const originalXhrOpen = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
        if (typeof url === 'string' && url.indexOf('/PlaybackInfo') !== -1 && url.indexOf('/FastSTRM/') === -1) {
            const match = url.match(/\/Items\/([a-zA-Z0-9]+)\/PlaybackInfo/i);
            if (match && match[1]) {
                const itemId = match[1];
                console.log(`[FastSTRM] Intercepted XHR PlaybackInfo request for ItemId: ${itemId}`);
                
                const urlObj = new URL(url, window.location.origin);
                const queryParams = urlObj.search;
                const originalUrlEnc = encodeURIComponent(urlObj.pathname + urlObj.search);
                
                url = `/FastSTRM/GetMockedPlaybackInfo?itemId=${itemId}&originalUrl=${originalUrlEnc}&${queryParams.substring(1)}`;
            }
        }
        return originalXhrOpen.call(this, method, url, async, user, password);
    };

    console.log("[FastSTRM] Successfully injected fetch and XHR interceptors.");
})();
