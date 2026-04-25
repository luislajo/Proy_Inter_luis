package com.example.intermodular.data.remote

import com.example.intermodular.BuildConfig
import com.example.intermodular.data.remote.auth.AuthInterceptor
import com.example.intermodular.data.remote.auth.SessionManager
import com.squareup.moshi.Moshi
import retrofit2.Retrofit
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import okhttp3.OkHttpClient
import retrofit2.converter.moshi.MoshiConverterFactory

/**
 * Objeto encargado de proporcionar una instancia del servicio de la API
 *
 * @author Axel Zaragoci y Ian Rodríguez
 */
object RetrofitProvider {

    /**
     * Configura y devuelve una instancia de Moshi para la conversión JSON
     * Incluye el adaptador personalizado para tratar con fechas
     */
    private fun moshi(): Moshi =
        Moshi.Builder()
            .add(InstantJsonAdapter())
            .add(KotlinJsonAdapterFactory())
            .build()

    /**
     * Cliente HTTP compartido (auth, timeouts). Misma instancia que usa Retrofit.
     */
    val httpClient: OkHttpClient by lazy {
        OkHttpClient.Builder()
            .addInterceptor(AuthInterceptor(SessionManager))
            .build()
    }

    /**
     * Configura y devuelve una instancia de Retrofit para la conexión a la API
     * Incluye el cliente HTTP y la instancia de Moshi personalizadas
     */
    private fun retrofit(): Retrofit =
        Retrofit.Builder()
            .baseUrl(BuildConfig.BASE_URL)
            .client(httpClient)
            .addConverterFactory(MoshiConverterFactory.create(moshi()))
            .build()


    /**
     * Crea y devuelve la instancia de ApiService a ser utilizada mediante el Retrofit configurado anteriormente
     */
    val api: ApiService by lazy {
        retrofit().create(ApiService::class.java)
    }
}