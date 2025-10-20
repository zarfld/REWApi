/**
 * REW API Basic Test
 * Test basic REW API endpoints to see what's available
 */

import REWApi from './src/js/rewApi.js';

async function testBasicEndpoints() {
    console.log('🔌 Testing basic REW API endpoints...');
    
    const api = new REWApi();
    
    const endpoints = [
        { name: 'getMeasurements', method: () => api.getMeasurements() },
        { name: 'getApplicationSettings', method: () => api.getApplicationSettings() },
        { name: 'getAudioSettings', method: () => api.getAudioSettings() },
        { name: 'getErrors', method: () => api.getErrors() },
        { name: 'getWarnings', method: () => api.getWarnings() }
    ];
    
    for (const endpoint of endpoints) {
        try {
            console.log(`📡 Testing ${endpoint.name}...`);
            const result = await endpoint.method();
            console.log(`✅ ${endpoint.name}:`, typeof result === 'object' ? JSON.stringify(result).substring(0, 100) + '...' : result);
        } catch (error) {
            console.log(`❌ ${endpoint.name}: ${error.message}`);
        }
    }
    
    // Test a simple measurement creation to see if REW accepts it
    try {
        console.log('\n📊 Testing measurement creation...');
        const testMeasurement = {
            name: "API Test Measurement",
            type: "sweep"
        };
        const result = await api.addMeasurement(testMeasurement);
        console.log('✅ Measurement creation successful:', result);
    } catch (error) {
        console.log('❌ Measurement creation failed:', error.message);
    }
}

// Run the test
testBasicEndpoints();