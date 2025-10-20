/**
 * REW API Connection Test
 * Test the real REW API with REW running locally
 */

import REWApi from './src/js/rewApi.js';

async function testREWConnection() {
    console.log('🔌 Testing REW API connection...');
    
    const api = new REWApi();
    
    try {
        // Test basic connection - get measurements
        console.log('📊 Testing getMeasurements()...');
        const measurements = await api.getMeasurements();
        console.log('✅ getMeasurements() successful:', measurements);
        
        // Test getting audio settings
        console.log('🔊 Testing getAudioSettings()...');
        const audioSettings = await api.getAudioSettings();
        console.log('✅ getAudioSettings() successful:', audioSettings);
        
        // Test getting application settings
        console.log('⚙️  Testing getApplicationSettings()...');
        const appSettings = await api.getApplicationSettings();
        console.log('✅ getApplicationSettings() successful:', appSettings);
        
        console.log('\n🎉 All tests passed! REW API is working correctly.');
        
    } catch (error) {
        console.error('❌ REW API connection failed:', error.message);
        console.log('\n💡 Make sure:');
        console.log('   1. REW is running');
        console.log('   2. REW API is enabled (Preferences > General > Enable API)');
        console.log('   3. REW is running on localhost:4735');
    }
}

// Run the test
testREWConnection();