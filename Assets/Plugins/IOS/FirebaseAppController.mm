#import "UnityAppController.h"

// Forward declaration
@interface FIRApp : NSObject
+ (void)configure;
@end

@interface UnityAppController (Firebase)
@end

@implementation UnityAppController (Firebase)

+ (void)load {
    // Questo metodo viene chiamato automaticamente quando la classe viene caricata
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        // Swizzling del metodo didFinishLaunchingWithOptions
        Class class = [self class];
        
        SEL originalSelector = @selector(application:didFinishLaunchingWithOptions:);
        SEL swizzledSelector = @selector(firebase_application:didFinishLaunchingWithOptions:);
        
        Method originalMethod = class_getInstanceMethod(class, originalSelector);
        Method swizzledMethod = class_getInstanceMethod(class, swizzledSelector);
        
        method_exchangeImplementations(originalMethod, swizzledMethod);
    });
}

- (BOOL)firebase_application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions {
    // Configura Firebase
    [FIRApp configure];
    
    // Chiama il metodo originale
    return [self firebase_application:application didFinishLaunchingWithOptions:launchOptions];
}

@end