/**
 * REW API Discovery Test
 * Discover available REW API endpoints
 */

import REWApi from './src/js/rewApi.js';

async function discoverREWEndpoints() {
    console.log('🔍 Discovering REW API endpoints...');
    
    const api = new REWApi();
    
    // Test the base URL
    try {
        const response = await fetch('http://localhost:4735/');
        console.log('📡 Base URL status:', response.status);
        if (response.ok) {
            const text = await response.text();
            console.log('📄 Base URL response:', text.substring(0, 200) + '...');
        }
    } catch (error) {
        console.log('❌ Base URL failed:', error.message);
    }
    
    // Test some common paths
    const commonPaths = [
        '',
        'api',
        'measurements',
        'audio',
        'application',
        'settings',
        'status',
        'info',
        'help',
        'version'
    ];
    
    for (const path of commonPaths) {
        try {
            const url = `http://localhost:4735/${path}`;
            const response = await fetch(url);
            if (response.ok) {
                const text = await response.text();
                console.log(`✅ /${path}: ${response.status} - ${text.substring(0, 100)}...`);
            } else {
                console.log(`❌ /${path}: ${response.status}`);
            }
        } catch (error) {
            console.log(`❌ /${path}: ${error.message}`);
        }
    }
}

// Run the discovery
discoverREWEndpoints();