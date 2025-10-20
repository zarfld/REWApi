/**
 * REW API Real Test
 * Test with actual working REW API endpoints
 */

import REWApi from './src/js/rewApi.js';

async function testRealREW() {
    console.log('🎯 Testing REW API with actual working endpoints...');
    
    const api = new REWApi();
    
    try {
        // Test version endpoint
        console.log('📋 Getting REW version...');
        const version = await api.get('/version');
        console.log('✅ REW Version:', version.message);
        
        // Test measurements endpoint
        console.log('\n📊 Getting measurements...');
        const measurements = await api.getMeasurements();
        console.log('✅ Measurements found:', Object.keys(measurements).length);
        
        // Show measurement details
        for (const [id, measurement] of Object.entries(measurements)) {
            console.log(`   📄 Measurement ${id}: "${measurement.title}" (${measurement.date})`);
            
            // Test getting frequency response for this measurement
            try {
                console.log(`   🎵 Getting frequency response for measurement ${id}...`);
                const freqResponse = await api.getFrequencyResponse(id);
                console.log(`   ✅ Frequency response: ${Object.keys(freqResponse).join(', ')}`);
            } catch (error) {
                console.log(`   ❌ Frequency response failed: ${error.message}`);
            }
            
            // Test getting impulse response for this measurement
            try {
                console.log(`   🎛️  Getting impulse response for measurement ${id}...`);
                const impulseResponse = await api.getImpulseResponse(id);
                console.log(`   ✅ Impulse response: ${Object.keys(impulseResponse).join(', ')}`);
            } catch (error) {
                console.log(`   ❌ Impulse response failed: ${error.message}`);
            }
            
            break; // Test with first measurement only
        }
        
        console.log('\n🎉 REW API testing completed successfully!');
        
    } catch (error) {
        console.error('❌ REW API test failed:', error.message);
    }
}

// Run the test
testRealREW();