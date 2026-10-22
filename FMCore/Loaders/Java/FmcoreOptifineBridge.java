// Copyright (c) 2026 Voltucit. MIT license. No OptiFine code is included.
// The embedded Java 8 class in OptiFineBridge.cs is compiled from this source, so a JRE is sufficient at runtime.
import java.io.File;
import java.lang.reflect.InvocationTargetException;
public final class FmcoreOptifineBridge {
    public static void main(String[] args) {
        try {
            if (args.length != 2) System.exit(42);
            Class<?> installer = Class.forName("optifine.Installer");
            String version = (String) installer.getMethod("getOptiFineVersion").invoke(null);
            if (!args[1].equals(version)) { System.err.println("OptiFine version mismatch."); System.exit(43); }
            installer.getMethod("doInstall", File.class).invoke(null, new File(args[0]));
        } catch (NoSuchMethodException e) {
            System.err.println("Unsupported OptiFine installer API."); System.exit(42);
        } catch (Throwable e) {
            Throwable cause = e instanceof InvocationTargetException ? ((InvocationTargetException)e).getCause() : e;
            cause.printStackTrace(); System.exit(1);
        }
    }
}
