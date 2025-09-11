#import "UnityAppController.h"
#import <objc/runtime.h>

// Forward declaration per Firebase
@interface FIRApp : NSObject
+ (void)configure;
@end

@implementation UnityAppController (Firebase)

+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        Class cls = NSClassFromString(@"UnityAppController");
        if (cls) {
            SEL originalSelector = @selector(application:didFinishLaunchingWithOptions:);
            SEL swizzledSelector = @selector(firebase_application:didFinishLaunchingWithOptions:);
            
            Method originalMethod = class_getInstanceMethod(cls, originalSelector);
            Method swizzledMethod = class_getInstanceMethod(cls, swizzledSelector);
            
            if (originalMethod && swizzledMethod) {
                BOOL didAddMethod = class_addMethod(cls, originalSelector,
                                                  method_getImplementation(swizzledMethod),
                                                  method_getTypeEncoding(swizzledMethod));
                
                if (didAddMethod) {
                    class_replaceMethod(cls, swizzledSelector,
                                      method_getImplementation(originalMethod),
                                      method_getTypeEncoding(originalMethod));
                } else {
                    method_exchangeImplementations(originalMethod, swizzledMethod);
                }
            }
        }
    });
}

- (BOOL)firebase_application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions {
    // Configura Firebase
    [FIRApp configure];
    
    // Chiama il metodo originale (ora swizzled)
    return [self firebase_application:application didFinishLaunchingWithOptions:launchOptions];
}

@end
